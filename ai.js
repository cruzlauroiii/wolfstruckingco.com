// WolfsAI — client for the Cloudflare Worker /ai endpoint that proxies Claude API calls.
// Falls back to a deterministic local template when the proxy returns "no-key" so the demo
// flow still completes; templates are clearly labelled so it's obvious when real AI is wired up.

(function(){
  const ENDPOINT = 'https://wolfstruckingco.nbth.workers.dev/ai';

  function getByoKey() {
    try { return localStorage.getItem('wolfs_anthropic_key') || ''; } catch { return ''; }
  }

  async function callProxy(messages, opts) {
    opts = opts || {};
    try {
      const headers = { 'Content-Type': 'application/json' };
      const byoKey = getByoKey();
      if (byoKey) headers['X-Anthropic-Key'] = byoKey;
      // Session headers — worker requires auth to prevent drive-by key abuse.
      const sess = localStorage.getItem('wolfs_session');
      const email = localStorage.getItem('wolfs_email');
      const role = localStorage.getItem('wolfs_role');
      if (sess) headers['X-Wolfs-Session'] = sess;
      if (email) headers['X-Wolfs-Email'] = email;
      if (role) headers['X-Wolfs-Role'] = role;
      const res = await fetch(ENDPOINT, {
        method: 'POST',
        headers,
        body: JSON.stringify({
          model: opts.model || 'claude-opus-4-7',
          system: opts.system || null,
          max_tokens: opts.max_tokens || 800,
          messages,
        }),
      });
      if (!res.ok) {
        const text = await res.text();
        return { ok: false, status: res.status, error: text };
      }
      const data = await res.json();
      return { ok: true, text: data.text, usage: data.usage };
    } catch (ex) {
      return { ok: false, status: 0, error: String(ex) };
    }
  }

  function templateProfileSummary(worker) {
    const role = (worker.roles && worker.roles[0]) || 'logistics worker';
    const yrs = worker.experienceYears || 5;
    const loc = worker.location || worker.address || 'regional';
    const goal = worker.careerGoals || 'career growth';
    const strengths = [];
    if (worker.badges && worker.badges.includes('bdg_hazmat')) strengths.push('hazmat-endorsed');
    if (worker.badges && worker.badges.includes('bdg_container')) strengths.push('container/intermodal');
    if (worker.badges && worker.badges.includes('bdg_twic')) strengths.push('TWIC-credentialed');
    if (yrs >= 8) strengths.push('senior experience');
    const gaps = [];
    if (!worker.badges || !worker.badges.includes('bdg_hazmat')) gaps.push('hazmat endorsement');
    if (!worker.badges || !worker.badges.includes('bdg_twic')) gaps.push('TWIC card');
    return (
      `**Summary (template fallback — no Claude API key configured):**\n\n` +
      `${worker.name || 'This worker'} is a ${yrs}-year ${role} based in ${loc}. ` +
      `Strengths: ${strengths.join(', ') || 'solid fundamentals'}. ` +
      `Gaps to address: ${gaps.join(', ') || 'none significant'}. ` +
      `Stated goal: "${goal}".\n\n` +
      `**Suggested next move:** Pursue a Lead Driver or Dispatcher role within ${loc} to build supervisor experience while staying in familiar territory.`
    );
  }

  function templateMatchRationale(worker, job) {
    const locMatch = worker.location && job.location && worker.location.split(',')[1]?.trim() === job.location.split(',')[1]?.trim();
    const yrs = worker.experienceYears || 0;
    const lines = [];
    lines.push(`${job.title} at ${job.company}${locMatch ? ' — local match' : ''}.`);
    if (yrs >= 5) lines.push(`${yrs}y experience aligns with the role's seniority expectations.`);
    if (worker.badges && worker.badges.includes('bdg_container') && /container|drayage|port/i.test(job.desc)) lines.push(`Container credentials matter directly here.`);
    return '_(template fallback)_ ' + lines.join(' ');
  }

  function templateChat(msg, worker) {
    return (
      `_(template fallback — no Claude API key configured)_\n\n` +
      `Regarding "${msg}": Given your ${worker && worker.experienceYears || 'N'}y experience and goal to ${worker && worker.careerGoals || 'grow'}, this looks like a reasonable fit if the commute and schedule work for you. Real agent responses require configuring ANTHROPIC_API_KEY in the Cloudflare Worker.`
    );
  }

  window.WolfsAI = {
    endpoint: ENDPOINT,
    getKey: getByoKey,
    setKey(k) { try { if (k) localStorage.setItem('wolfs_anthropic_key', k); else localStorage.removeItem('wolfs_anthropic_key'); } catch {} },
    hasKey() { return !!getByoKey(); },
    async generateProfileSummary(worker) {
      const res = await callProxy([
        { role: 'user', content: `Write a 3-paragraph career coach summary for this worker. Identify strengths, gaps, and a specific suggested next move. Be concrete and warm, not generic.\n\nWorker profile:\n${JSON.stringify(worker, null, 2)}` }
      ], { system: 'You are a senior career coach for logistics and supply-chain workers. Responses are grounded in the profile data provided — never hallucinate credentials or employment history.', max_tokens: 700 });
      if (res.ok) return res.text;
      return templateProfileSummary(worker);
    },
    async generateMatchRationale(worker, job) {
      const res = await callProxy([
        { role: 'user', content: `In 2 sentences, explain why this job fits this worker. Reference their specific experience and any credential alignment. No hype, no platitudes.\n\nWorker:\n${JSON.stringify(worker)}\n\nJob:\n${JSON.stringify(job)}` }
      ], { system: 'You are a thoughtful AI recruiter. Be concrete and truthful.', max_tokens: 200 });
      if (res.ok) return res.text;
      return templateMatchRationale(worker, job);
    },
    async chat(worker, job, history, message, systemOverride) {
      const sys = systemOverride || `You are a personal career agent working on behalf of ${worker.name || 'the worker'}. Their profile: ${JSON.stringify(worker)}. When a specific job is referenced in the conversation, it is: ${JSON.stringify(job)}. Be proactive — suggest actions, not just information. Reference specifics from the profile.`;
      const messages = (history || []).concat([{ role: 'user', content: message }]);
      const res = await callProxy(messages, { system: sys, max_tokens: 600 });
      if (res.ok) return res.text;
      return templateChat(message, worker);
    },
    async generateCandidateSummary(worker, role) {
      const res = await callProxy([
        { role: 'user', content: `Write a 2-sentence candidate summary pitching this worker to a hiring manager for this role. Reference the worker's specific strengths.\n\nWorker: ${JSON.stringify(worker)}\nRole: ${JSON.stringify(role)}` }
      ], { system: 'You write concise, truthful candidate summaries for hiring managers.', max_tokens: 250 });
      if (res.ok) return res.text;
      return `_(template fallback)_ ${worker.name} — ${worker.experienceYears || 'experienced'}y in ${(worker.roles || [])[0] || 'logistics'}, credentials: ${(worker.badges || []).length}. Good fit for ${role.name || role.title || 'the role'}.`;
    },
  };
})();
