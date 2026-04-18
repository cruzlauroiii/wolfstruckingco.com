// Subscriber-side live location renderer for employer / dispatcher / staff.
//   const sub = WolfsTrackSub.attach({ map: leafletMap, driverId: 'wkr_driver', onUpdate: cb });
//   sub.stop();
//
// Opens a Server-Sent Events stream to the worker's /api/loc/<id>/stream endpoint, animates
// a green truck pin between updates, and auto-reconnects when the 90-second SSE window closes.

(function (global) {
  'use strict';

  const RELAY = 'https://wolfstruckingco.nbth.workers.dev';

  function attach(opts) {
    const map = opts && opts.map;
    const driverId = opts && opts.driverId;
    const onUpdate = (opts && opts.onUpdate) || (() => {});
    const onConnect = (opts && opts.onConnect) || (() => {});
    if (!map || !driverId) throw new Error('WolfsTrackSub.attach: map + driverId required');
    if (typeof L === 'undefined') throw new Error('Leaflet (L) not loaded');

    const truckHtml = '<div style="width:32px;height:32px;border-radius:50%;background:#22c55e;border:3px solid #0f1419;display:flex;align-items:center;justify-content:center;color:#fff;font-size:1rem;box-shadow:0 0 0 4px rgba(34,197,94,.3);transition:transform .4s ease-out">🚚</div>';
    const truckIcon = L.divIcon({ className: '', html: truckHtml, iconSize: [32, 32], iconAnchor: [16, 16] });

    let marker = null;
    let lastFix = null;
    let es = null;
    let stopped = false;

    function place(fix) {
      const ll = [fix.lat, fix.lng];
      if (!marker) {
        marker = L.marker(ll, { icon: truckIcon, zIndexOffset: 1000 }).addTo(map);
        try { map.setView(ll, Math.max(map.getZoom() || 12, 13)); } catch (_) {}
      } else {
        marker.setLatLng(ll);
      }
      // Rotate the inner div by `heading` so the truck visually faces direction of travel.
      try {
        const inner = marker._icon && marker._icon.firstChild;
        if (inner && fix.heading != null) inner.style.transform = 'rotate(' + fix.heading + 'deg)';
      } catch (_) {}
      lastFix = fix;
      onUpdate(fix);
    }

    function connect() {
      if (stopped) return;
      try { es && es.close(); } catch (_) {}
      es = new EventSource(RELAY + '/api/loc/' + encodeURIComponent(driverId) + '/stream');
      es.addEventListener('open', () => onConnect(true));
      es.addEventListener('error', () => {
        // EventSource auto-reconnects on transient errors; on close (after our 90s window),
        // open a fresh one so the pin keeps animating.
        if (es && es.readyState === EventSource.CLOSED) setTimeout(connect, 500);
      });
      es.addEventListener('open', () => {});
      es.addEventListener('loc', (ev) => {
        try {
          const fix = JSON.parse(ev.data);
          place(fix);
        } catch (_) {}
      });
    }

    // Seed with the last-known point so the pin shows up before the first SSE tick.
    fetch(RELAY + '/api/loc/' + encodeURIComponent(driverId), { cache: 'no-store' })
      .then(r => r.ok ? r.json() : null)
      .then(p => { if (p && !lastFix && !stopped) place(p); })
      .catch(() => {});

    connect();

    return {
      stop() {
        stopped = true;
        try { es && es.close(); } catch (_) {}
        try { marker && map.removeLayer(marker); } catch (_) {}
      },
      lastFix: () => lastFix,
    };
  }

  global.WolfsTrackSub = { attach };
})(typeof window !== 'undefined' ? window : globalThis);
