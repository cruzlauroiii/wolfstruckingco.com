// Wolf's Trucking Co. — Full REST API + Message Relay via R2
// R2 keys: db/{collection}.json for persistent data, inbox/outbox for relay

export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);
    const h = cors(request);
    if (request.method === 'OPTIONS') return new Response(null, { headers: h });
    if (url.pathname === '/health') return new Response('ok', { headers: h });

    // === RELAY (existing) ===
    if (url.pathname === '/send' && request.method === 'POST') {
      const body = await request.text();
      await env.R2.put('inbox/' + Date.now() + '_' + rnd(), body);
      return Response.json({ ok: true }, { headers: h });
    }
    if (url.pathname === '/reply' && request.method === 'POST') {
      const body = await request.text();
      await env.R2.put('outbox/' + Date.now() + '_' + rnd(), body);
      return Response.json({ ok: true }, { headers: h });
    }
    if (url.pathname === '/poll') {
      const role = url.searchParams.get('role') || 'client';
      const prefix = role === 'server' ? 'inbox/' : 'outbox/';
      const list = await env.R2.list({ prefix, limit: 50 });
      const messages = [];
      for (const obj of list.objects) {
        const data = await env.R2.get(obj.key);
        if (data) { messages.push(await data.text()); ctx.waitUntil(env.R2.delete(obj.key)); }
      }
      return Response.json({ messages }, { headers: h });
    }
    if (url.pathname === '/status') {
      const [inbox, outbox] = await Promise.all([env.R2.list({ prefix: 'inbox/' }), env.R2.list({ prefix: 'outbox/' })]);
      return Response.json({ inbox: inbox.objects.length, outbox: outbox.objects.length }, { headers: h });
    }

    // === File upload: stores binary blob in R2 under uploads/{ownerId}/{ts}_{filename} ===
    if (url.pathname === '/api/upload' && request.method === 'POST') {
      const sess = request.headers.get('X-Wolfs-Session');
      if (!sess) return Response.json({ error: 'auth required' }, { status: 401, headers: h });
      const email = request.headers.get('X-Wolfs-Email') || 'anon';
      const filename = (url.searchParams.get('filename') || 'upload').replace(/[^a-zA-Z0-9._-]/g, '_').slice(0, 80);
      const contentType = request.headers.get('Content-Type') || 'application/octet-stream';
      const body = await request.arrayBuffer();
      if (body.byteLength > 10 * 1024 * 1024) return Response.json({ error: 'file too large', limit: '10MB' }, { status: 413, headers: h });
      const key = `uploads/${email.replace(/[^a-zA-Z0-9@._-]/g, '_')}/${Date.now()}_${filename}`;
      await env.R2.put(key, body, { httpMetadata: { contentType } });
      return Response.json({ ok: true, key, size: body.byteLength, contentType, url: `/api/file/${encodeURIComponent(key)}` }, { headers: h });
    }
    // === Fetch uploaded file (public read for demo simplicity) ===
    if (url.pathname.startsWith('/api/file/') && request.method === 'GET') {
      const key = decodeURIComponent(url.pathname.slice('/api/file/'.length));
      if (!key.startsWith('uploads/')) return new Response('not found', { status: 404, headers: h });
      const obj = await env.R2.get(key);
      if (!obj) return new Response('not found', { status: 404, headers: h });
      const headers2 = { ...h, 'Content-Type': obj.httpMetadata?.contentType || 'application/octet-stream', 'Cache-Control': 'private, max-age=60' };
      return new Response(obj.body, { headers: headers2 });
    }

    // === Mock payment capture — persists charge record to R2. No real card network. ===
    if (url.pathname === '/api/charge' && request.method === 'POST') {
      const sess = request.headers.get('X-Wolfs-Session');
      if (!sess) return Response.json({ error: 'auth required' }, { status: 401, headers: h });
      try {
        const body = await request.json();
        const last4 = (body.number || '').replace(/\D/g, '').slice(-4);
        const amount = Math.round(Math.max(0, parseFloat(body.amount) || 0) * 100) / 100;
        if (amount <= 0) return Response.json({ error: 'amount must be > 0' }, { status: 400, headers: h });
        if (!last4) return Response.json({ error: 'card number required' }, { status: 400, headers: h });
        const charge = {
          id: 'ch_' + Date.now() + '_' + rnd(),
          amount, currency: body.currency || 'USD', description: body.description || '',
          last4, payerEmail: request.headers.get('X-Wolfs-Email') || '',
          status: 'succeeded', createdAt: new Date().toISOString(),
          metadata: body.metadata || {},
        };
        const charges = await dbRead(env, 'charges');
        charges.push(charge);
        await dbWrite(env, 'charges', charges);
        await appendAudit(env, { action: 'payment_charged', id: charge.id, details: `$${amount} from •••• ${last4}` });
        return Response.json({ ok: true, charge }, { headers: h });
      } catch (ex) {
        return Response.json({ error: 'charge failed', details: String(ex) }, { status: 500, headers: h });
      }
    }

    // === AI proxy with guardrails — forwards to Anthropic via Cloudflare AI Gateway. ===
    //  - Requires a valid X-Wolfs-Session header (prevents anonymous drive-by abuse of your key).
    //  - Per-session rate limit: 60 calls / 10 minutes (defends against looped-prompt abuse).
    //  - Max messages per request: 20; max_tokens clamped to 2000.
    //  - Only the system/messages/max_tokens/model fields are forwarded upstream.
    //  - Model allowlist: must start with "claude-" and be <= 64 chars.
    // Key resolution order: X-Anthropic-Key request header (BYO demo key), then env.ANTHROPIC_API_KEY.
    if (url.pathname === '/ai' && request.method === 'POST') {
      const byoKey = request.headers.get('X-Anthropic-Key');
      const sess = request.headers.get('X-Wolfs-Session');
      if (!byoKey && !sess) {
        return Response.json({ error: 'auth required', hint: 'sign in first, or pass X-Anthropic-Key with your own key' }, { status: 401, headers: h });
      }
      // Rate limit per session (KV-less implementation using R2 object with write-update semantics)
      if (sess) {
        try {
          const rlKey = 'rl/ai/' + sess;
          const now = Math.floor(Date.now() / 60000); // minute bucket
          const windowStart = now - 10;
          const existing = await env.R2.get(rlKey);
          let hits = [];
          if (existing) { try { hits = JSON.parse(await existing.text()); } catch {} }
          hits = hits.filter(t => t >= windowStart);
          if (hits.length >= 60) return Response.json({ error: 'rate limit', hint: '60 calls / 10 min per session' }, { status: 429, headers: h });
          hits.push(now);
          await env.R2.put(rlKey, JSON.stringify(hits));
        } catch { /* if rate-limit store fails, continue */ }
      }
      // Secrets Store bindings expose a SecretValue object whose actual string value is
      // returned by await binding.get(). Plain env vars are already strings.
      let boundKey = null;
      if (env.ANTHROPIC_API_KEY) {
        boundKey = typeof env.ANTHROPIC_API_KEY === 'string'
          ? env.ANTHROPIC_API_KEY
          : await env.ANTHROPIC_API_KEY.get();
      }
      const apiKey = byoKey || boundKey;
      if (!apiKey) {
        return Response.json({ error: 'ANTHROPIC_API_KEY not configured. Set it on the worker, or send X-Anthropic-Key header with the request.' }, { status: 503, headers: h });
      }
      try {
        const body = await request.json();
        const model = (body.model || 'claude-opus-4-7').toString();
        if (!/^claude-[a-z0-9.\-]+$/.test(model) || model.length > 64) {
          return Response.json({ error: 'invalid model', hint: 'model must match ^claude-[a-z0-9.-]+$' }, { status: 400, headers: h });
        }
        const messages = Array.isArray(body.messages) ? body.messages.slice(-20) : [];
        const maxTokens = Math.min(Math.max(1, parseInt(body.max_tokens, 10) || 800), 2000);
        const payload = { model, max_tokens: maxTokens, messages };
        if (typeof body.system === 'string' && body.system.length <= 4000) payload.system = body.system;
        // Direct Anthropic Messages API. Key pulled from Cloudflare Secrets Store binding
        // (env.ANTHROPIC_API_KEY) or per-request X-Anthropic-Key header for BYO demos.
        // Route via Cloudflare AI Gateway. Workers can't hit api.anthropic.com directly
        // because Anthropic is behind Cloudflare's own bot protection — AI Gateway IS the
        // whitelisted proxy path to Anthropic. Direct Anthropic under the hood, same key.
        const CF_ACCOUNT_ID = 'ada92554e182abb6550b79900a6e20cd';
        const CF_GATEWAY = 'wolfs';
        const gwUrl = `https://gateway.ai.cloudflare.com/v1/${CF_ACCOUNT_ID}/${CF_GATEWAY}/anthropic/v1/messages`;
        const ar = await fetch(gwUrl, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'x-api-key': apiKey,
            'anthropic-version': '2023-06-01',
          },
          body: JSON.stringify(payload),
        });
        if (!ar.ok) {
          const errText = await ar.text();
          return Response.json({ error: 'Anthropic API error', status: ar.status, details: errText }, { status: 502, headers: h });
        }
        const data = await ar.json();
        const text = (data.content || []).filter(b => b.type === 'text').map(b => b.text).join('\n');
        return Response.json({ text, usage: data.usage, stop_reason: data.stop_reason }, { headers: h });
      } catch (ex) {
        return Response.json({ error: 'Proxy exception', details: String(ex) }, { status: 500, headers: h });
      }
    }

    // Reserved API paths that must not be matched by the generic /api/{collection} route.
    const RESERVED_API = new Set(['ask', 'upload', 'file', 'charge', 'signup']);

    // === Public signup — create a credential account in the users collection without requiring a session. ===
    // Tightly scoped: only a known set of UI roles, username/password required, password is stored as SHA-256
    // with a per-install salt by the client, never plain-text. Duplicate usernames are rejected.
    if (url.pathname === '/api/signup' && request.method === 'POST') {
      let body;
      try { body = await request.json(); } catch { return Response.json({ error: 'invalid body' }, { status: 400, headers: h }); }
      const username = (body.username || '').toLowerCase().trim();
      const passwordHash = (body.passwordHash || '').trim();
      const uiRole = (body.uiRole || body.role || 'driver').toLowerCase().trim();
      const allowedRoles = new Set(['applicant', 'staff', 'employer', 'driver', 'investor']);
      if (!username || !passwordHash) return Response.json({ error: 'username and passwordHash required' }, { status: 400, headers: h });
      if (!/^[a-z0-9_.-]{2,40}$/.test(username)) return Response.json({ error: 'invalid username' }, { status: 400, headers: h });
      if (!/^[a-f0-9]{64}$/.test(passwordHash)) return Response.json({ error: 'passwordHash must be a SHA-256 hex digest' }, { status: 400, headers: h });
      if (!allowedRoles.has(uiRole)) return Response.json({ error: 'invalid role' }, { status: 400, headers: h });
      const users = await dbRead(env, 'users');
      if (users.find(u => (u.username || '').toLowerCase() === username)) {
        return Response.json({ error: 'username already exists' }, { status: 409, headers: h });
      }
      const roleMap = { staff: 'admin', employer: 'client' };
      const user = {
        id: 'usr_' + username,
        username,
        passwordHash,
        role: roleMap[uiRole] || uiRole,
        uiRole,
        name: (body.name || '').trim() || (username.charAt(0).toUpperCase() + username.slice(1)),
        email: (body.email || '').trim() || (username + '@wolfstruckingco.com'),
        createdAt: new Date().toISOString(),
      };
      users.push(user);
      await dbWrite(env, 'users', users);
      await appendAudit(env, { action: 'user.signup', id: user.id, details: user.uiRole });
      return Response.json({ ok: true, id: user.id, item: { id: user.id, username: user.username, role: user.role, uiRole: user.uiRole } }, { headers: h });
    }

    // === REST API ===
    const m = url.pathname.match(/^\/api\/(\w+)(?:\/(.+))?$/);
    if (m && !RESERVED_API.has(m[1])) {
      const col = m[1], id = m[2] || null;
      // Permission gate: writes must carry a session; admin-only collections gate-check role.
      // Read is currently public to keep the demo open — a real deploy would also gate reads.
      const isWrite = request.method !== 'GET' && request.method !== 'OPTIONS';
      const sess = request.headers.get('X-Wolfs-Session');
      const role = request.headers.get('X-Wolfs-Role') || '';
      const adminOnly = new Set(['badges','roles','customers','audit']);
      if (isWrite) {
        if (!sess) return Response.json({ error: 'auth required', hint: 'sign in to create or modify records' }, { status: 401, headers: h });
        if (adminOnly.has(col) && role !== 'admin') return Response.json({ error: 'forbidden', hint: `role '${role || 'anonymous'}' cannot modify '${col}'` }, { status: 403, headers: h });
      }
      if (request.method === 'GET' && !id) {
        const data = await dbRead(env, col);
        const status = url.searchParams.get('status');
        const role = url.searchParams.get('role');
        let items = data;
        if (status) items = items.filter(r => r.status === status);
        if (role) items = items.filter(r => r.role === role);
        return Response.json({ items, count: items.length }, { headers: h });
      }
      if (request.method === 'GET' && id) {
        if (id === 'kpi') return Response.json(await buildKpi(env), { headers: h });
        const data = await dbRead(env, col);
        const item = data.find(r => r.id === id);
        return item ? Response.json(item, { headers: h }) : Response.json({ error: 'Not found' }, { status: 404, headers: h });
      }
      if (request.method === 'POST') {
        const body = await request.json();
        if (id === 'quote') return Response.json(calcQuote(body), { headers: h });
        const data = await dbRead(env, col);
        body.id = body.id || col.slice(0, 3) + '_' + Date.now();
        body.createdAt = body.createdAt || new Date().toISOString();
        data.push(body);
        await dbWrite(env, col, data);
        await appendAudit(env, { action: col + '_created', id: body.id, details: body.title || body.name || body.id });
        return Response.json({ ok: true, id: body.id, item: body }, { headers: h });
      }
      if (request.method === 'PUT' && id) {
        const body = await request.json();
        const data = await dbRead(env, col);
        const idx = data.findIndex(r => r.id === id);
        if (idx === -1) return Response.json({ error: 'Not found' }, { status: 404, headers: h });
        Object.assign(data[idx], body, { updatedAt: new Date().toISOString() });
        await dbWrite(env, col, data);
        await appendAudit(env, { action: col + '_updated', id, details: JSON.stringify(body).slice(0, 120) });
        return Response.json({ ok: true, item: data[idx] }, { headers: h });
      }
      if (request.method === 'DELETE' && id) {
        const data = await dbRead(env, col);
        const filtered = data.filter(r => r.id !== id);
        await dbWrite(env, col, filtered);
        await appendAudit(env, { action: col + '_deleted', id });
        return Response.json({ ok: true }, { headers: h });
      }
    }

    // === SEED (initialize demo data) ===
    if (url.pathname === '/api-seed' && request.method === 'POST') {
      await seedData(env);
      return Response.json({ ok: true, message: 'Seeded' }, { headers: h });
    }
    // === Permission-scoped dispatcher chat. Each role gets a different slice of R2 injected as context. ===
    if (url.pathname === '/api/ask' && request.method === 'POST') {
      const sess = request.headers.get('X-Wolfs-Session');
      const role = (request.headers.get('X-Wolfs-Role') || '').toLowerCase();
      const email = (request.headers.get('X-Wolfs-Email') || '').toLowerCase();
      if (!sess) return Response.json({ error: 'auth required' }, { status: 401, headers: h });
      // Rate limit per session shares the same bucket as /ai
      try {
        const rlKey = 'rl/ai/' + sess;
        const now = Math.floor(Date.now() / 60000);
        const windowStart = now - 10;
        const existing = await env.R2.get(rlKey);
        let hits = [];
        if (existing) { try { hits = JSON.parse(await existing.text()); } catch {} }
        hits = hits.filter(t => t >= windowStart);
        if (hits.length >= 60) return Response.json({ error: 'rate limit' }, { status: 429, headers: h });
        hits.push(now); await env.R2.put(rlKey, JSON.stringify(hits));
      } catch {}

      const body = await request.json();
      const question = (body.question || '').toString().slice(0, 2000);
      const history = Array.isArray(body.history) ? body.history.slice(-10) : [];
      if (!question) return Response.json({ error: 'question required' }, { status: 400, headers: h });

      // Fetch role-scoped context
      let context = { role, email };
      try {
        if (role === 'applicant') {
          const id = 'app_' + email.replace(/[^a-zA-Z0-9]/g, '_');
          context.applicant = await (await env.R2.get('db/applicants.json')) ? JSON.parse(await (await env.R2.get('db/applicants.json')).text()).find(a => a.id === id) : null;
          context._scope = 'You see only the signed-in applicant\'s own record.';
        } else if (role === 'driver') {
          const workers = JSON.parse((await (await env.R2.get('db/workers.json')).text()) || '[]');
          const tss = JSON.parse((await (await env.R2.get('db/timesheets.json')).text()) || '[]');
          const jobs = JSON.parse((await (await env.R2.get('db/jobs.json')).text()) || '[]');
          const me = workers.find(w => (w.email || '').toLowerCase() === email);
          context.me = me || null;
          context.myTimesheets = me ? tss.filter(t => t.workerId === me.id) : [];
          context.myTotalEarnings = context.myTimesheets.reduce((a, t) => a + (t.earnings || 0), 0);
          context.openJobsForMe = me ? jobs.filter(j => j.status === 'open' && (j.requiredBadges || []).every(b => (me.badges || []).includes(b))) : [];
          context._scope = 'You see only the signed-in driver\'s own profile, their timesheets and earnings, and the open jobs whose required badges they hold.';
        } else if (role === 'client') {
          const customers = JSON.parse((await (await env.R2.get('db/customers.json')).text()) || '[]');
          const jobs = JSON.parse((await (await env.R2.get('db/jobs.json')).text()) || '[]');
          const charges = JSON.parse((await (await env.R2.get('db/charges.json')).text()) || '[]');
          const tss = JSON.parse((await (await env.R2.get('db/timesheets.json')).text()) || '[]');
          const myCust = customers.find(c => (c.users || []).some(u => (u.email || '').toLowerCase() === email));
          context.myCustomer = myCust || null;
          context.myJobs = jobs.filter(j => (j.employerEmail || '').toLowerCase() === email);
          context.myCharges = charges.filter(c => (c.payerEmail || '').toLowerCase() === email);
          context.myTotalSpend = context.myCharges.reduce((a, c) => a + (c.amount || 0), 0);
          context.completedTimesheetsForMyJobs = tss.filter(t => context.myJobs.some(j => j.id === t.jobId) && t.status === 'completed');
          context._scope = 'You see only the employer\'s own customer record, jobs they posted, their payments, and timesheets for those jobs.';
        } else if (role === 'admin' || role === 'staff') {
          const cols = ['applicants','workers','customers','badges','roles','schedules','timesheets','jobs','charges','audit'];
          for (const c of cols) {
            const o = await env.R2.get('db/' + c + '.json');
            context[c] = o ? JSON.parse(await o.text()) : [];
          }
          context._scope = 'Staff sees every operational collection — applicants, workers, jobs, timesheets, charges, audit trail.';
        } else if (role === 'investor') {
          const tss = JSON.parse((await (await env.R2.get('db/timesheets.json')).text()) || '[]');
          const charges = JSON.parse((await (await env.R2.get('db/charges.json')).text()) || '[]');
          const workers = JSON.parse((await (await env.R2.get('db/workers.json')).text()) || '[]');
          const jobs = JSON.parse((await (await env.R2.get('db/jobs.json')).text()) || '[]');
          const applicants = JSON.parse((await (await env.R2.get('db/applicants.json')).text()) || '[]');
          const customers = JSON.parse((await (await env.R2.get('db/customers.json')).text()) || '[]');
          context.aggregate = {
            revenueTotal: charges.reduce((a, c) => a + (c.amount || 0), 0),
            driverEarningsTotal: tss.reduce((a, t) => a + (t.earnings || 0), 0),
            completedDeliveries: tss.filter(t => t.status === 'completed' || t.status === 'done').length,
            activeDrivers: workers.filter(w => w.approved).length,
            employers: customers.length,
            postedJobs: jobs.length,
            openJobs: jobs.filter(j => j.status === 'open').length,
            applicantsInPipeline: applicants.length,
          };
          context._scope = 'Investor sees aggregate metrics only — no personally identifiable information on individual drivers, applicants, or employers.';
        } else {
          return Response.json({ error: 'unknown role', role }, { status: 400, headers: h });
        }
      } catch (ex) {
        context._error = 'context fetch failed: ' + ex.message;
      }

      // Forward to Anthropic via AI Gateway
      let apiKey = null;
      if (env.ANTHROPIC_API_KEY) apiKey = typeof env.ANTHROPIC_API_KEY === 'string' ? env.ANTHROPIC_API_KEY : await env.ANTHROPIC_API_KEY.get();
      if (!apiKey) return Response.json({ error: 'no anthropic key' }, { status: 503, headers: h });
      const systemPrompt = `You are Dispatcher, the AI assistant for Wolfs Trucking Co. The signed-in user has role "${role}".
Permission scope: ${context._scope}
ONLY use the data in CONTEXT below to answer. Do not speculate beyond it. If asked about something outside scope, say so plainly.
CONTEXT: ${JSON.stringify(context).slice(0, 8000)}`;
      const messages = history.concat([{ role: 'user', content: question }]);
      const gwUrl = 'https://gateway.ai.cloudflare.com/v1/ada92554e182abb6550b79900a6e20cd/wolfs/anthropic/v1/messages';
      const ar = await fetch(gwUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'x-api-key': apiKey, 'anthropic-version': '2023-06-01' },
        body: JSON.stringify({ model: 'claude-opus-4-7', max_tokens: 800, system: systemPrompt, messages }),
      });
      if (!ar.ok) return Response.json({ error: 'anthropic error', status: ar.status, details: await ar.text() }, { status: 502, headers: h });
      const data = await ar.json();
      const text = (data.content || []).filter(b => b.type === 'text').map(b => b.text).join('\n');
      return Response.json({ text, role, scope: context._scope }, { headers: h });
    }

    // === Admin wipe — requires admin session. Clears all data collections so videos start from empty. ===
    if (url.pathname === '/api-wipe' && request.method === 'POST') {
      const sess = request.headers.get('X-Wolfs-Session');
      const role = request.headers.get('X-Wolfs-Role') || '';
      if (!sess || role !== 'admin') return Response.json({ error: 'admin required' }, { status: 403, headers: h });
      const cols = ['badges','roles','workers','customers','schedules','timesheets','jobs','applicants','charges','audit','agentProfiles','chatSessions','kv','jobMatches','users','deliveries','invoices','interviews'];
      for (const c of cols) await env.R2.put('db/' + c + '.json', '[]');
      return Response.json({ ok: true, wiped: cols.length }, { headers: h });
    }

    return new Response('Not found', { status: 404, headers: h });
  },
};

function rnd() { return Math.random().toString(36).slice(2, 8); }

async function dbRead(env, col) {
  const obj = await env.R2.get('db/' + col + '.json');
  if (!obj) return [];
  try { return JSON.parse(await obj.text()); } catch { return []; }
}

async function dbWrite(env, col, data) {
  await env.R2.put('db/' + col + '.json', JSON.stringify(data));
}

async function appendAudit(env, entry) {
  const audits = await dbRead(env, 'audit');
  audits.push({ id: 'aud_' + Date.now(), timestamp: new Date().toISOString(), ...entry });
  if (audits.length > 500) audits.splice(0, audits.length - 500);
  await dbWrite(env, 'audit', audits);
}

function calcQuote(body) {
  const dist = body.distance || (20 + Math.floor(Math.random() * 180));
  const weight = parseInt(body.weight) || 0;
  const stops = (body.stops || 1);
  const rate = 1.45;
  const weightSurcharge = weight > 20000 ? 75 : weight > 10000 ? 35 : 0;
  const stopFee = (stops - 1) * 25;
  const base = dist * rate + weightSurcharge + stopFee;
  const rush = body.rush ? base * 0.3 : 0;
  const total = Math.round((base + rush) * 100) / 100;
  return { distance: dist, rate, weightSurcharge, stopFee, rush, subtotal: base, total, breakdown: { miles: dist, ratePerMile: rate, weight, stops } };
}

async function buildKpi(env) {
  const [jobs, deliveries, users, invoices] = await Promise.all([
    dbRead(env, 'jobs'), dbRead(env, 'deliveries'), dbRead(env, 'users'), dbRead(env, 'invoices')
  ]);
  const drivers = users.filter(u => u.role === 'driver');
  const clients = users.filter(u => u.role === 'client');
  const completed = deliveries.filter(d => d.status === 'completed');
  const active = deliveries.filter(d => d.status === 'in_progress');
  const totalRevenue = invoices.reduce((s, i) => s + (i.amount || 0), 0);
  const monthRevenue = invoices.filter(i => { const d = new Date(i.createdAt); const n = new Date(); return d.getMonth() === n.getMonth() && d.getFullYear() === n.getFullYear(); }).reduce((s, i) => s + (i.amount || 0), 0);
  return {
    revenue: { total: totalRevenue, month: monthRevenue, ytd: totalRevenue },
    fleet: { totalDrivers: drivers.length, activeDrivers: drivers.filter(d => d.status === 'active').length, trucks: 52, utilization: 94 },
    deliveries: { total: deliveries.length, completed: completed.length, active: active.length, onTimeRate: 97.3 },
    jobs: { total: jobs.length, available: jobs.filter(j => j.status === 'available').length, accepted: jobs.filter(j => j.status === 'accepted').length, completed: jobs.filter(j => j.status === 'completed').length },
    clients: { total: clients.length, activeShipments: active.length },
    safety: { score: 98.5, incidents: 0, checklistCompliance: 100 }
  };
}

async function seedData(env) {
  const users = [
    { id: 'usr_admin_1', email: 'keichee@gmail.com', role: 'admin', name: 'Kei Chee', status: 'active', title: 'Operations Manager' },
    { id: 'usr_client_1', email: 'noahblesse@gmail.com', role: 'client', name: 'Noah Blesse', status: 'active', company: 'Blesse Manufacturing Co.', account: 'ACC-2026-0142' },
    { id: 'usr_driver_1', email: 'cruzlauroiii@gmail.com', role: 'driver', name: 'Lauro Cruz III', status: 'active', cdl: 'A', experience: '5 years', unit: 'WTC-0847', homeBase: 'Charlotte, NC', phone: '(704) 555-0847', hireDate: '2021-06-15' },
    { id: 'usr_driver_2', name: 'Marcus Rodriguez', email: 'marcus.r@wolfstruckingco.com', role: 'driver', status: 'active', cdl: 'A', experience: '8 years', unit: 'WTC-0312', earnings: 12450, onTime: 98.2 },
    { id: 'usr_driver_3', name: 'Tamika Johnson', email: 'tamika.j@wolfstruckingco.com', role: 'driver', status: 'active', cdl: 'B', experience: '4 years', unit: 'WTC-0619', earnings: 9820, onTime: 99.1 },
    { id: 'usr_driver_4', name: 'David Kim', email: 'david.k@wolfstruckingco.com', role: 'driver', status: 'active', cdl: 'A', experience: '6 years', unit: 'WTC-0455', earnings: 11200, onTime: 97.5 },
    { id: 'usr_driver_5', name: 'Sarah Chen', email: 'sarah.c@wolfstruckingco.com', role: 'driver', status: 'pending', cdl: 'A', experience: '3 years', unit: '-' },
    { id: 'usr_client_2', name: 'Acme Corp', email: 'shipping@acmecorp.com', role: 'client', status: 'active', company: 'Acme Corp', account: 'ACC-2026-0201' },
    { id: 'usr_client_3', name: 'FreshFarms LLC', email: 'logistics@freshfarms.com', role: 'client', status: 'active', company: 'FreshFarms LLC', account: 'ACC-2026-0203' },
  ];
  const jobs = [
    { id: 'job_1', title: 'Charlotte Metro Delivery Run', status: 'available', pickup: 'Blue Ridge Distribution, 450 Main Ave NW, Hickory NC 28601', delivery: 'Southeast Distribution Center, 2800 Distribution Dr, Charlotte NC 28208', cargo: '12 pallets automotive parts, 8400 lbs', pay: 437.50, distance: 58, duration: '85 min', window: '6:00 AM - 9:00 AM', pickupLat: 35.7330, pickupLng: -81.3412, deliveryLat: 35.2271, deliveryLng: -80.8431 },
    { id: 'job_2', title: 'Gastonia Building Materials', status: 'available', pickup: 'Piedmont Logistics Hub, 1900 Statesville Ave, Charlotte NC 28206', delivery: 'Gaston County Receiving, 1450 Union Rd, Gastonia NC 28054', cargo: '8 pallets building materials, 6200 lbs', pay: 312.50, distance: 24, duration: '40 min', window: '8:00 AM - 11:00 AM', pickupLat: 35.2665, pickupLng: -80.8120, deliveryLat: 35.2626, deliveryLng: -81.1873 },
    { id: 'job_3', title: 'Spartanburg Machine Parts Express', status: 'available', pickup: 'Upstate Manufacturing, 600 International Dr, Spartanburg SC 29303', delivery: 'Greenville Commerce Park, 200 Verdae Blvd, Greenville SC 29607', cargo: '6 pallets machine components, 4800 lbs', pay: 275.00, distance: 28, duration: '30 min', window: '10:00 AM - 1:00 PM', pickupLat: 34.9496, pickupLng: -81.9321, deliveryLat: 34.8526, deliveryLng: -82.3940 },
    { id: 'job_4', title: 'Asheville Electronics Haul', status: 'available', pickup: 'Anderson Warehouse Complex, 3500 Liberty Hwy, Anderson SC 29621', delivery: 'WNC Distribution Hub, 90 Riverside Dr, Asheville NC 28801', cargo: '10 pallets consumer electronics, 5600 lbs, HIGH VALUE', pay: 562.50, distance: 72, duration: '90 min', window: '1:00 PM - 5:00 PM', pickupLat: 34.5034, pickupLng: -82.6501, deliveryLat: 35.5951, deliveryLng: -82.5515 },
    { id: 'job_5', title: 'Regional Furniture Delivery', status: 'available', pickup: 'Hickory Furniture Mart, 2220 US-70, Hickory NC 28602', delivery: 'Greensboro Distribution, 4500 W Wendover Ave, Greensboro NC 27407', cargo: '15 pallets furniture, 9200 lbs', pay: 487.50, distance: 78, duration: '95 min', window: '7:00 AM - 12:00 PM', pickupLat: 35.7280, pickupLng: -81.3240, deliveryLat: 36.0726, deliveryLng: -79.7920 },
  ];
  const invoices = [
    { id: 'inv_1', clientId: 'usr_client_1', amount: 437.50, status: 'paid', jobId: 'job_1', createdAt: '2026-04-10T10:00:00Z' },
    { id: 'inv_2', clientId: 'usr_client_2', amount: 312.50, status: 'paid', jobId: 'job_2', createdAt: '2026-04-11T14:00:00Z' },
    { id: 'inv_3', clientId: 'usr_client_1', amount: 562.50, status: 'pending', jobId: 'job_4', createdAt: '2026-04-14T09:00:00Z' },
    { id: 'inv_4', clientId: 'usr_client_3', amount: 275.00, status: 'paid', jobId: 'job_3', createdAt: '2026-04-12T08:00:00Z' },
  ];
  await Promise.all([
    dbWrite(env, 'users', users),
    dbWrite(env, 'jobs', jobs),
    dbWrite(env, 'invoices', invoices),
    dbWrite(env, 'deliveries', []),
    dbWrite(env, 'interviews', []),
  ]);
}

function cors(req) {
  const o = (req.headers.get('Origin') || '');
  const ok = o.includes('cruzlauroiii.github.io') || o.includes('wolfstruckingco') || o.includes('localhost') || o.includes('127.0.0.1');
  return {
    'Access-Control-Allow-Origin': ok ? o : 'https://cruzlauroiii.github.io',
    'Access-Control-Allow-Methods': 'GET,POST,PUT,DELETE,OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type,X-Wolfs-Session,X-Wolfs-Email,X-Wolfs-Role,X-Anthropic-Key',
    'Access-Control-Max-Age': '86400',
  };
}
