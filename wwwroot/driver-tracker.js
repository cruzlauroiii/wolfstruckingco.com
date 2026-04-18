// Driver-side live location writer.
//   const tracker = WolfsTracker.start({ driverId: 'wkr_driver', jobId: 'job_xyz', intervalMs: 3000, onError: console.warn });
//   tracker.stop();
//
// Runs navigator.geolocation.watchPosition + a Web Worker timer that POSTs the latest fix
// to the Cloudflare Worker every `intervalMs`. The Web Worker timer is what keeps the loop
// alive when the driver tab is backgrounded (Chrome throttles main-thread timers to ~1/min
// on hidden tabs, but workers are exempt).

(function (global) {
  'use strict';

  const RELAY = 'https://wolfstruckingco.nbth.workers.dev';

  // Inline Web Worker source. Posting it as a Blob URL avoids needing a separate hosted file.
  const WORKER_SRC = `
    let id = null;
    self.addEventListener('message', (ev) => {
      const { type, intervalMs } = ev.data || {};
      if (type === 'start') {
        if (id) clearInterval(id);
        id = setInterval(() => self.postMessage({ type: 'tick' }), Math.max(500, intervalMs || 3000));
      } else if (type === 'stop') {
        if (id) clearInterval(id);
        id = null;
      }
    });
  `;

  function start(opts) {
    const driverId = (opts && opts.driverId) || (localStorage.getItem('wolfs_driver_id') || 'wkr_driver');
    const jobId = (opts && opts.jobId) || null;
    const intervalMs = (opts && opts.intervalMs) || 3000;
    const onError = (opts && opts.onError) || (() => {});
    const onUpdate = (opts && opts.onUpdate) || (() => {});

    if (!('geolocation' in navigator)) {
      onError(new Error('Geolocation not supported in this browser'));
      return { stop() {}, isActive: () => false };
    }

    let lastFix = null;
    let watchId = null;
    let worker = null;
    let workerUrl = null;
    let active = true;

    // Watch the device GPS continuously. Each fix updates `lastFix`; the Web Worker's tick
    // posts that latest fix to the server, so we don't burn requests when the device is still.
    try {
      watchId = navigator.geolocation.watchPosition(
        (pos) => {
          lastFix = {
            lat: pos.coords.latitude,
            lng: pos.coords.longitude,
            heading: pos.coords.heading,
            speed: pos.coords.speed,
            accuracy: pos.coords.accuracy,
          };
          onUpdate(lastFix);
        },
        (err) => onError(err),
        { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
      );
    } catch (ex) { onError(ex); }

    async function postFix() {
      if (!active || !lastFix) return;
      try {
        await fetch(RELAY + '/api/loc/' + encodeURIComponent(driverId), {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ ...lastFix, jobId }),
          keepalive: true,
        });
      } catch (ex) { onError(ex); }
    }

    try {
      const blob = new Blob([WORKER_SRC], { type: 'application/javascript' });
      workerUrl = URL.createObjectURL(blob);
      worker = new Worker(workerUrl);
      worker.addEventListener('message', (ev) => {
        if (ev.data && ev.data.type === 'tick') postFix();
      });
      worker.postMessage({ type: 'start', intervalMs });
    } catch (ex) {
      // Fall back to main-thread timer (will throttle on hidden tabs but better than nothing).
      onError(new Error('Web Worker unavailable; using main-thread timer (background tab will throttle): ' + (ex.message || ex)));
      const id = setInterval(postFix, intervalMs);
      worker = { terminate() { clearInterval(id); }, postMessage() {} };
    }

    return {
      stop() {
        active = false;
        try { if (watchId != null) navigator.geolocation.clearWatch(watchId); } catch (_) {}
        try { worker && worker.postMessage({ type: 'stop' }); } catch (_) {}
        try { worker && worker.terminate && worker.terminate(); } catch (_) {}
        try { workerUrl && URL.revokeObjectURL(workerUrl); } catch (_) {}
      },
      isActive: () => active,
      lastFix: () => lastFix,
    };
  }

  // Simulator for demo / video — moves the driver along a list of [lat,lng] coords without
  // requiring real GPS permission. Same POST shape as the real writer.
  function simulate(opts) {
    const driverId = (opts && opts.driverId) || 'wkr_driver';
    const jobId = (opts && opts.jobId) || null;
    const path = (opts && opts.path) || [];
    const tickMs = (opts && opts.tickMs) || 800;
    const onUpdate = (opts && opts.onUpdate) || (() => {});
    if (!path.length) return { stop() {} };
    let i = 0;
    let stopped = false;
    async function tick() {
      if (stopped) return;
      const [lat, lng] = path[i];
      const next = path[Math.min(i + 1, path.length - 1)];
      const heading = bearing([lat, lng], next);
      const fix = { lat, lng, heading, speed: 12, accuracy: 5, jobId };
      try {
        await fetch(RELAY + '/api/loc/' + encodeURIComponent(driverId), {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(fix),
        });
      } catch (_) {}
      onUpdate(fix);
      i++;
      if (i < path.length) setTimeout(tick, tickMs);
    }
    tick();
    return { stop() { stopped = true; } };
  }

  function bearing(a, b) {
    if (!a || !b) return 0;
    const toRad = (d) => d * Math.PI / 180;
    const toDeg = (r) => r * 180 / Math.PI;
    const y = Math.sin(toRad(b[1] - a[1])) * Math.cos(toRad(b[0]));
    const x = Math.cos(toRad(a[0])) * Math.sin(toRad(b[0])) - Math.sin(toRad(a[0])) * Math.cos(toRad(b[0])) * Math.cos(toRad(b[1] - a[1]));
    return (toDeg(Math.atan2(y, x)) + 360) % 360;
  }

  global.WolfsTracker = { start, simulate };
})(typeof window !== 'undefined' ? window : globalThis);
