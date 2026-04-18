// Shared dispatcher chat — one implementation, every page.
//
//   <div id="Chat"></div>
//   <script src="/wolfstruckingco.com/dispatcher-chat.js"></script>
//   <script>
//     WolfsChat.mount(document.getElementById('Chat'), {
//       role: 'applicant',                   // required — drives scope + system prompt
//       systemPrompt: '…optional override…',
//       inlineUploads: true,                 // render upload dock when AI asks for a credential
//       onTranscriptChange: (transcript) => { /* persist wherever */ },
//     });
//   </script>
//
// Covers: chat log, input + Send button, Call button with multi-state feedback
// (Idle / Loading / Listening / Speaking / Processing / Replying), voice sidecar health
// retry, Aria TTS playback, inline credential upload when the AI asks, auto-grow log.
// Used by /Dispatcher/, /Applicant/, /CareerAgent/, and anywhere else the dispatcher chat
// surfaces so there's one source of truth.

(function (global) {
  'use strict';

  const SIDECAR = (location.protocol==='https:'?'/sidecar':'http://localhost:9334');
  const RELAY  = 'https://wolfstruckingco.nbth.workers.dev';

  // Three-role scope prompts. Legacy roles fold to one of these via normalizeRole().
  const SCOPE_PROMPTS = {
    admin:  'You are Wolfs Trucking\'s AI dispatcher talking to an admin. Scope: every operational collection — applicants, workers, jobs, timesheets, charges, listings, purchases, and the audit trail.',
    driver: 'You are Wolfs Trucking\'s AI dispatcher talking to a hired driver. Scope: their profile, timesheets, total earnings, and open jobs matching their badges. No other drivers\' data.',
    user:   'You are Wolfs Trucking\'s AI dispatcher talking to a platform user. Scope: only what belongs to them — their applicant record (if any), the jobs they posted, payments they made, listings they sell, and items they bought. Never reveal cross-user data.',
  };

  function normalizeRole(role) {
    const r = (role || '').toLowerCase();
    if (r === 'admin' || r === 'driver' || r === 'user') return r;
    if (r === 'staff') return 'admin';
    return 'user'; // applicant, employer, client, investor, anything else
  }

  // Deterministic dummy replies used on localhost. Keeps video rebuilds + local dev fast and
  // free of real API calls. Replies match the narration in scenes-full.json.
  function dummyReplyFor(role, question) {
    const q = (question || '').toLowerCase();
    const r = normalizeRole(role);
    if (r === 'admin') {
      if (q.includes('pending') || q.includes('applicant') || q.includes('queue')) {
        return 'There are **0 applicants pending review** right now — the queue is clear. Jordan Vega was the last one approved.';
      }
      if (q.includes('revenue') || q.includes('charge') || q.includes('aggregate') || q.includes('platform') || q.includes('status')) {
        return 'Platform-wide right now: **$49** in posting revenue, **$280** paid out to drivers, **1** completed delivery, **2** posted jobs, **1** marketplace listing, and **1** marketplace purchase pending pickup.';
      }
      return 'I can pull operational data — applicants, workers, jobs, timesheets, charges, listings, purchases, or the audit trail. What do you need?';
    }
    if (r === 'driver') {
      if (q.includes('earn') || q.includes('work') || q.includes('job')) {
        return 'Hey Jordan! Here\'s the rundown:\n\n**Today\'s Earnings**\n- 1 completed timesheet logged: **8 hours @ $35/hr = $280**\n- That matches your total earnings on file: **$280**\n\n**Open Work For You**\n- 1 marketplace pickup ready: chair from Wilshire Blvd to Beverly Hills.\n\nReady to roll when you are.';
      }
      return 'Ask me about your earnings, open jobs matching your badges, or a specific timesheet — I can pull whatever\'s on your driver record.';
    }
    // user
    if (q.includes('status') || q.includes('next step')) {
      return 'Your application is in the admin review queue. I have your CDL-A front, TWIC card, and DOT medical exam on file. Next step is admin verification — you\'ll hear back once badges are assigned.';
    }
    if (q.includes('order') || q.includes('purchase') || q.includes('bought')) {
      return 'You have 1 active order — the Refurbished Office Chair, $185, paying cash on delivery. Status: pickup job posted, waiting on a driver to accept.';
    }
    return 'Ask me about your application status, jobs you posted, listings you\'re selling, or items you bought — I\'ll pull the real numbers.';
  }

  const CSS = `
  .WChat{display:flex;flex-direction:column;gap:10px;background:var(--card,#1a2332);border:1px solid var(--border,#2a3a4a);border-radius:12px;padding:14px;width:100%}
  .WChat .WChatLog{flex:1;min-height:200px;max-height:60vh;overflow-y:auto;display:flex;flex-direction:column;gap:10px;padding:4px}
  .WChat .WMsg{max-width:90%;padding:10px 14px;border-radius:10px;line-height:1.45;font-size:.92rem;word-wrap:break-word;white-space:pre-wrap}
  .WChat .WMsg.User{align-self:flex-end;background:var(--accent,#ff6b35);color:#fff}
  .WChat .WMsg.Agent{align-self:flex-start;background:rgba(255,107,53,.08);border:1px solid rgba(255,107,53,.3);color:var(--text,#e8e8e8)}
  .WChat .WMsg .WLabel{display:block;font-size:.7rem;font-weight:700;opacity:.7;text-transform:uppercase;letter-spacing:.4px;margin-bottom:4px}
  .WChat .WForm{display:flex;gap:8px;align-items:stretch}
  .WChat .WForm input{flex:1;padding:11px 14px;border-radius:10px;background:var(--bg,#0f1419);border:1px solid var(--border,#2a3a4a);color:var(--text,#e8e8e8);font-family:inherit;font-size:.95rem}
  .WChat .WForm input:focus{border-color:var(--accent,#ff6b35);outline:none}
  .WChat .WForm button{padding:11px 16px;border-radius:10px;font-weight:700;cursor:pointer;font-family:inherit;font-size:.88rem;border:1px solid transparent;flex-shrink:0}
  .WChat .WForm .WCall{background:var(--accent,#ff6b35);color:#fff;border-color:var(--accent,#ff6b35)}
  .WChat .WForm .WCall:hover{background:#ff8c5a;border-color:#ff8c5a}
  .WChat .WForm .WCall.Loading{background:#9ca3af;border-color:#9ca3af;color:#fff;animation:WSpin 1s linear infinite}
  .WChat .WForm .WCall.Listening{background:#ef4444;border-color:#ef4444;color:#fff}
  .WChat .WForm .WCall.Speaking{background:#22c55e;border-color:#22c55e;color:#fff;animation:WPulse 1s infinite}
  .WChat .WForm .WCall.Processing{background:#9ca3af;border-color:#9ca3af;color:#fff}
  .WChat .WForm .WCall.Replying{background:#3b82f6;border-color:#3b82f6;color:#fff;animation:WPulse 1.2s infinite}
  .WChat .WForm .WSend{background:var(--accent,#ff6b35);color:#fff;border-color:var(--accent,#ff6b35)}
  .WChat .WStatus{font-size:.74rem;color:var(--text-muted,#8899aa);min-height:1em}
  .WChat .WInlineUpload{display:flex;gap:8px;align-items:center;margin-top:8px;padding:8px 10px;background:rgba(255,107,53,.08);border:1px dashed var(--accent,#ff6b35);border-radius:8px;font-size:.78rem;flex-wrap:wrap}
  .WChat .WInlineUpload>span{flex:1;min-width:160px;color:var(--accent,#ff6b35);font-weight:600}
  .WChat .WInlineUpload label{padding:6px 12px;border-radius:6px;background:var(--accent,#ff6b35);color:#fff;font-weight:700;cursor:pointer}
  .WChat .Scope{font-size:.76rem;color:var(--text-muted,#8899aa);padding:6px 10px;background:rgba(255,255,255,.03);border-radius:6px}
  .WChat .Spinner{display:inline-block;width:12px;height:12px;border-radius:50%;border:2px solid rgba(255,107,53,.25);border-top-color:var(--accent,#ff6b35);animation:WSpin .7s linear infinite;margin-right:6px;vertical-align:middle}
  @keyframes WSpin{to{transform:rotate(360deg)}}
  @keyframes WPulse{0%,100%{box-shadow:0 0 0 0 rgba(255,255,255,.35)}50%{box-shadow:0 0 0 10px rgba(255,255,255,0)}}
  `;

  function installCss() {
    if (document.getElementById('WChatStyles')) return;
    const el = document.createElement('style');
    el.id = 'WChatStyles';
    el.textContent = CSS;
    document.head.appendChild(el);
  }

  function escapeHtml(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }

  function uploadHintFor(text) {
    const t = (text || '').toLowerCase();
    if (/\bcdl\b|class a|class b|license/.test(t)) return { kind:'cdl', label:'CDL / license' };
    if (/\btwic\b|port|mtsa/.test(t))              return { kind:'twic', label:'TWIC card' };
    if (/\bmedical\b|doctor|fmcsa|certificate/.test(t)) return { kind:'medical', label:'DOT medical cert' };
    if (/\bhazmat\b|endorse/.test(t))              return { kind:'hazmat', label:'Hazmat endorsement' };
    if (/upload|scan|attach|document/.test(t))     return { kind:'other', label:'document' };
    return null;
  }

  function mount(container, opts) {
    if (!container) throw new Error('WolfsChat.mount requires a container element');
    opts = opts || {};
    const role = (opts.role || 'driver').toLowerCase();
    const inlineUploads = opts.inlineUploads !== false;
    const systemPrompt = opts.systemPrompt || SCOPE_PROMPTS[role] || SCOPE_PROMPTS.driver;
    const scopeBlurb = opts.scopeBlurb || '';

    installCss();

    container.classList.add('WChat');
    container.innerHTML = `
      ${scopeBlurb ? '<div class="Scope">'+escapeHtml(scopeBlurb)+'</div>' : ''}
      <div class="WChatLog" data-log></div>
      <form class="WForm" data-form>
        <input data-input placeholder="Ask Dispatcher…" autocomplete="off" required>
        <button type="button" class="WCall" data-call>&#9742; Call</button>
        <button type="submit" class="WSend">Send</button>
      </form>
      <div class="WStatus" data-status></div>
    `;

    const logEl   = container.querySelector('[data-log]');
    const form    = container.querySelector('[data-form]');
    const input   = container.querySelector('[data-input]');
    const callBtn = container.querySelector('[data-call]');
    const statusEl = container.querySelector('[data-status]');

    const transcript = (opts.transcript || []).slice();
    let callActive = false;
    let rec = null, stream = null, chunks = [], audioEl = null;
    let vadCtx = null, vadRaf = null;

    function setCallState(state, text) {
      callBtn.classList.remove('Loading','Listening','Speaking','Processing','Replying');
      const map = { idle:'☎ Call', loading:'⌛ Starting…', listening:'☎ End', speaking:'🎤 Speaking…', processing:'⏳ Processing…', replying:'🔊 Dispatcher…' };
      callBtn.textContent = map[state] || '☎ Call';
      if (state !== 'idle') callBtn.classList.add(state.charAt(0).toUpperCase() + state.slice(1));
      if (text != null) statusEl.textContent = text;
    }

    function render() {
      logEl.innerHTML = '';
      for (const m of transcript) {
        const d = document.createElement('div');
        d.className = 'WMsg ' + (m.role === 'user' ? 'User' : 'Agent');
        const hint = (!inlineUploads || m.role !== 'assistant') ? null : uploadHintFor(m.content);
        let upload = '';
        if (hint) {
          upload = `<div class="WInlineUpload"><span>📎 Upload your ${escapeHtml(hint.label)} here</span><label>Choose file<input type="file" accept="image/*,application/pdf,image/svg+xml" style="display:none" data-inline-upload data-kind="${hint.kind}" data-label="${escapeHtml(hint.label)}"></label><a href="/wolfstruckingco.com/Documents/" target="_blank" style="color:var(--text-muted);font-size:.72rem;text-decoration:none">Need a sample?</a></div>`;
        }
        d.innerHTML = `<span class="WLabel">${m.role === 'user' ? 'You' : 'Dispatcher'}</span>${escapeHtml(m.content)}${upload}`;
        logEl.appendChild(d);
      }
      // Wire up any inline upload inputs that just rendered
      Array.from(logEl.querySelectorAll('[data-inline-upload]')).forEach(inp => {
        inp.addEventListener('change', (ev) => {
          const f = ev.target.files && ev.target.files[0];
          if (!f) return;
          const kind = ev.target.dataset.kind;
          const label = ev.target.dataset.label;
          transcript.push({ role: 'user', content: `📎 Attached ${f.name} (${Math.round(f.size/1024)} KB) for ${label}.`, ts: new Date().toISOString() });
          if (typeof opts.onUpload === 'function') opts.onUpload({ file: f, kind, label });
          if (typeof opts.onTranscriptChange === 'function') opts.onTranscriptChange(transcript);
          render();
        });
      });
      logEl.scrollTop = logEl.scrollHeight;
    }

    async function sendMessage(text) {
      text = (text || '').trim();
      if (!text) return;
      transcript.push({ role: 'user', content: text, ts: new Date().toISOString() });
      render();
      if (typeof opts.onUserMessage === 'function') opts.onUserMessage(text);

      // Show a spinner bubble.
      const pending = document.createElement('div');
      pending.className = 'WMsg Agent';
      pending.innerHTML = '<span class="WLabel">Dispatcher</span><span class="Spinner"></span>Thinking…';
      logEl.appendChild(pending);
      logEl.scrollTop = logEl.scrollHeight;

      try {
        const sess = localStorage.getItem('wolfs_session') || '';
        const email = localStorage.getItem('wolfs_email') || (role + '@wolfstruckingco.com');
        const userRole = (localStorage.getItem('wolfs_role') || role).toLowerCase();
        const headers = { 'Content-Type': 'application/json' };
        if (sess) headers['X-Wolfs-Session'] = sess;
        if (email) headers['X-Wolfs-Email'] = email;
        headers['X-Wolfs-Role'] = userRole;
        const history = transcript.slice(-12).map(m => ({ role: m.role === 'user' ? 'user' : 'assistant', content: m.content }));
        let reply = '';
        // Localhost short-circuit: return a canned dummy reply so video rebuilds and local
        // dev don't burn real API calls. Production (cruzlauroiii.github.io) still calls the
        // worker and gets real grounded answers.
        const isLocal = /^(localhost|127\.|\[::1\])/.test(location.hostname) || location.hostname === '';
        if (isLocal) {
          reply = dummyReplyFor(userRole, text);
        } else {
          // Prefer /api/ask — the worker loads real R2 data and injects it as CONTEXT so answers
          // are grounded. If it fails (e.g. the deployed worker doesn't yet handle this role),
          // fall back to the raw /ai relay with a client-side scope prompt so the UI still replies.
          try {
            const question = (text || '').toString();
            const ask = await fetch(RELAY + '/api/ask', {
              method: 'POST', headers,
              body: JSON.stringify({ question, history: history.slice(0, -1) }),
            });
            if (ask.ok) {
              const d = await ask.json();
              reply = d.text || d.answer || '';
            }
          } catch (_) { /* fall through to /ai */ }
          if (!reply) {
            const res = await fetch(RELAY + '/ai', {
              method: 'POST', headers,
              body: JSON.stringify({ model: 'claude-opus-4-7', system: systemPrompt, max_tokens: 600, messages: history }),
            });
            const data = await res.json();
            reply = data.text || data.error || 'No reply.';
          }
        }
        pending.remove();
        transcript.push({ role: 'assistant', content: reply, ts: new Date().toISOString() });
        render();
        if (typeof opts.onAgentReply === 'function') opts.onAgentReply(reply);
        if (typeof opts.onTranscriptChange === 'function') opts.onTranscriptChange(transcript);
      } catch (ex) {
        pending.remove();
        transcript.push({ role: 'assistant', content: 'Dispatcher is unavailable: ' + (ex.message || ex), ts: new Date().toISOString() });
        render();
      }
    }

    // ─── Voice pipeline ────────────────────────────────────────────────────
    async function checkSidecar() {
      try {
        const r = await fetch(SIDECAR + '/health', { cache: 'no-store' });
        return r.ok;
      } catch { return false; }
    }

    async function speak(text) {
      try {
        // Kill any previous playback BEFORE starting the new one, so barge-in always has
        // a live reference to the currently-sounding audio element.
        try { if (audioEl) { audioEl.pause(); audioEl = null; } } catch (_) {}
        setCallState('replying', '🔊 Dispatcher is replying out loud…');
        const r = await fetch(SIDECAR + '/tts', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ text }) });
        if (!r.ok) return;
        const blob = await r.blob();
        const el = new Audio(URL.createObjectURL(blob));
        audioEl = el;
        try { await el.play(); } catch (_) {}
        await new Promise(res => { el.onended = res; el.onerror = res; setTimeout(res, 20000); });
      } catch {}
    }

    // One AudioContext/analyser for the entire call — re-creating per chunk caused
    // chunks 2+ to stall silently in Chromium. Per-chunk VAD state (speaking/silence
    // counters) gets reset by resetVAD() instead.
    let vadAn = null;
    let vadSpeaking = false, vadSilence = 0, vadEver = false;
    function resetVAD() { vadSpeaking = false; vadSilence = 0; vadEver = false; }
    function startVAD() {
      resetVAD();
      try {
        if (!vadCtx) {
          vadCtx = new (window.AudioContext || window.webkitAudioContext)();
          const src = vadCtx.createMediaStreamSource(stream);
          vadAn = vadCtx.createAnalyser();
          vadAn.fftSize = 512;
          src.connect(vadAn);
          console.log('[WolfsChat VAD] AudioContext created, state=', vadCtx.state);
        }
        if (vadCtx.state === 'suspended') { vadCtx.resume().catch(() => {}); }
        const buf = new Uint8Array(vadAn.frequencyBinCount);
        let logCounter = 0;
        const tick = () => {
          if (!callActive) { vadRaf = null; return; }
          vadAn.getByteTimeDomainData(buf);
          let sum = 0; for (let i = 0; i < buf.length; i++) { const v = (buf[i] - 128) / 128; sum += v * v; }
          const rms = Math.sqrt(sum / buf.length);
          if (++logCounter % 60 === 0) console.log('[WolfsChat VAD] rms=', rms.toFixed(4), 'speaking=', vadSpeaking, 'silence=', vadSilence, 'recording=', !!rec && rec.state === 'recording');
          if (rms > 0.015) {
            vadSilence = 0;
            // Barge-in every frame while the user is speaking, so any speak() that
            // started AFTER the onset still gets killed.
            try { if (audioEl && !audioEl.paused) { audioEl.pause(); audioEl.currentTime = 0; } } catch (_) {}
            // Safety net: pause ANY <audio> element in the document, in case a prior speak()
            // leaked past the single closure reference.
            try { document.querySelectorAll('audio').forEach(a => { if (!a.paused) { a.pause(); try { a.currentTime = 0; } catch (_) {} } }); } catch (_) {}
            if (!vadSpeaking) {
              vadSpeaking = true; vadEver = true;
              setCallState('speaking', '🎤 I hear you — keep talking, pause when done');
              console.log('[WolfsChat VAD] speech onset, rms=', rms.toFixed(4));
              if (!rec || rec.state !== 'recording') startRecordingNow();
            }
          } else {
            vadSilence++;
            if (vadSpeaking && vadSilence > 50) { vadSpeaking = false; setCallState('listening', '🔴 Listening — pause a bit more to send, or click End'); }
            if (vadEver && !vadSpeaking && vadSilence > 90 && rec && rec.state === 'recording') {
              console.log('[WolfsChat VAD] silence — finalising chunk');
              setCallState('processing', '⏳ Processing…');
              try { rec.requestData(); } catch (_) {}
              try { rec.stop(); } catch (_) {}
              vadEver = false;
            }
          }
          vadRaf = requestAnimationFrame(tick);
        };
        if (!vadRaf) { console.log('[WolfsChat VAD] tick starting'); tick(); }
      } catch (ex) { console.error('[WolfsChat VAD] startVAD failed:', ex); }
    }

    function startRecordingNow() {
      try {
        const mimeCandidates = ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus'];
        const mimeType = mimeCandidates.find(m => typeof MediaRecorder !== 'undefined' && MediaRecorder.isTypeSupported(m)) || '';
        const localRec = mimeType
          ? new MediaRecorder(stream, { mimeType, audioBitsPerSecond: 96000 })
          : new MediaRecorder(stream);
        const localChunks = [];
        localRec.ondataavailable = ev => { if (ev.data && ev.data.size) localChunks.push(ev.data); };
        localRec.onstop = () => {
          const blob = new Blob(localChunks, { type: mimeType || 'audio/webm' });
          console.log('[WolfsChat VAD] recorder onstop, blob=', Math.round(blob.size/1024), 'KB');
          if (blob.size >= 1500) processChunkBlob(blob);
          if (callActive) setCallState('listening', '🔴 Listening — speak anytime');
        };
        localRec.start();
        rec = localRec;
        console.log('[WolfsChat VAD] recorder started');
      } catch (ex) { console.error('[WolfsChat VAD] startRecordingNow failed:', ex); }
    }
    function stopVAD() {
      try { if (vadRaf) cancelAnimationFrame(vadRaf); } catch {}
      vadRaf = null;
    }
    function teardownVAD() {
      stopVAD();
      try { if (vadCtx) vadCtx.close(); } catch {}
      vadCtx = null; vadAn = null;
    }

    // Each chunk's transcribe+send runs in parallel with the next chunk's recording,
    // so the mic never blocks while an earlier phrase is still being processed. Multiple
    // chunks can be in-flight at once; they land in the chat in completion order.
    async function processChunkBlob(blob) {
      try {
        const r = await fetch(SIDECAR + '/stt', { method: 'POST', headers: { 'Content-Type': 'audio/webm' }, body: blob });
        if (!r.ok) throw new Error('stt ' + r.status);
        const { text } = await r.json();
        if (!text || !text.trim()) return;
        await sendMessage(text);
        const last = transcript[transcript.length - 1];
        if (last && last.role === 'assistant') { speak(last.content).catch(() => {}); }
      } catch (ex) { /* surface error in status, but don't block next chunk */ }
    }
    // Legacy hook — recording is now VAD-driven in startRecordingNow().
    function startChunk() { /* intentionally empty */ }
    async function startCall() {
      setCallState('loading', '⌛ Starting mic…');
      const ok = await checkSidecar();
      if (!ok) { setCallState('idle', '⚠️ Voice sidecar unreachable — click Call to retry.'); return; }
      try {
        if (!stream) stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        callActive = true;
        setCallState('listening', '🔴 Listening — speak anytime, click End to hang up');
        startChunk();
        startVAD();
      } catch { setCallState('idle', '❌ Mic access denied.'); }
    }
    function stopCurrentChunk() {
      // Flush the tail buffer before stopping, otherwise the last ~200ms is lost.
      try { if (rec && rec.state === 'recording') { try { rec.requestData(); } catch (_) {} rec.stop(); } } catch (_) {}
    }
    function hangup() {
      callActive = false;
      teardownVAD();
      try { if (rec && rec.state === 'recording') rec.stop(); } catch {}
      try { if (stream) stream.getTracks().forEach(t => t.stop()); } catch {}
      try { if (audioEl) { audioEl.pause(); audioEl = null; } } catch (_) {}
      stream = null; rec = null; chunks = [];
      setCallState('idle', '✅ Call ended.');
    }

    callBtn.addEventListener('click', () => { if (callActive) hangup(); else startCall(); });
    form.addEventListener('submit', async (ev) => {
      ev.preventDefault();
      const v = input.value.trim();
      if (!v) return;
      input.value = '';
      await sendMessage(v);
    });

    render();
    return { sendMessage, getTranscript: () => transcript.slice(), element: container };
  }

  global.WolfsChat = { mount, SCOPE_PROMPTS };
})(typeof window !== 'undefined' ? window : this);
