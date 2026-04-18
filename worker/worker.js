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

    // === REST API ===
    const m = url.pathname.match(/^\/api\/(\w+)(?:\/(.+))?$/);
    if (m) {
      const col = m[1], id = m[2] || null;
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
    'Access-Control-Allow-Headers': 'Content-Type',
    'Access-Control-Max-Age': '86400',
  };
}
