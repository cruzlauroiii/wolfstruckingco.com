// Thin JS bridge for the Blazor DispatcherChat component. Blazor calls
// WolfsChatVoice.start() to open the mic + begin recording, and WolfsChatVoice.stop()
// to stop recording, POST the audio to the voice sidecar's /stt endpoint, and return
// the transcribed text back to .NET as a string (empty if nothing was captured).

window.WolfsChatVoice = (function () {
  const SIDECAR = (location.protocol === 'https:' ? '/sidecar' : 'http://localhost:9334');
  let stream = null, rec = null, chunks = [];

  async function start() {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) return false;
    try { const r = await fetch(SIDECAR + '/health', { cache: 'no-store' }); if (!r.ok) throw 0; } catch { return false; }
    try { stream = await navigator.mediaDevices.getUserMedia({ audio: true }); }
    catch { return false; }
    rec = new MediaRecorder(stream);
    chunks = [];
    rec.ondataavailable = ev => { if (ev.data && ev.data.size) chunks.push(ev.data); };
    rec.start();
    return true;
  }

  async function stop() {
    if (!rec) return '';
    await new Promise(resolve => {
      rec.onstop = resolve;
      try { rec.stop(); } catch { resolve(); }
    });
    try { stream && stream.getTracks().forEach(t => t.stop()); } catch {}
    stream = null; rec = null;
    if (!chunks.length) return '';
    const blob = new Blob(chunks, { type: 'audio/webm' });
    try {
      const r = await fetch(SIDECAR + '/stt', { method: 'POST', headers: { 'Content-Type': 'audio/webm' }, body: blob });
      if (!r.ok) return '';
      const j = await r.json();
      return (j && j.text) || '';
    } catch { return ''; }
  }

  async function speak(text) {
    if (!text) return;
    try {
      const r = await fetch(SIDECAR + '/tts', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ text }) });
      if (!r.ok) return;
      const blob = await r.blob();
      const audio = new Audio(URL.createObjectURL(blob));
      audio.play().catch(() => {});
    } catch {}
  }

  return { start, stop, speak };
})();
