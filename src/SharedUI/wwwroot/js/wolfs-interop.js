// Single JS interop module loaded by SharedUI components. Wraps Leaflet, IndexedDB, and the
// Cloudflare worker fetch under a stable API the C# side calls via IJSRuntime.
// No third-party JS — Leaflet itself is loaded via <script> in index.html and is the only dep.

(function () {
  if (window.WolfsInterop) return;
  const w = {};

  const WORKER = 'https://wolfstruckingco.nbth.workers.dev';

  // ─── Map (Leaflet) ─────────────────────────────────────────────────────
  const maps = new Map();
  w.mapInit = function (id, lat, lng, zoom, theme) {
    if (!window.L) throw new Error('Leaflet not loaded');
    if (maps.has(id)) { try { maps.get(id).remove(); } catch (_) {} maps.delete(id); }
    const m = L.map(id, { zoomControl: false, maxZoom: 19 }).setView([lat, lng], zoom);
    L.control.zoom({ position: 'bottomleft' }).addTo(m);
    const tileUrl = theme === 'dark'
      ? 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'
      : 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png';
    L.tileLayer(tileUrl, { maxZoom: 19, subdomains: 'abcd' }).addTo(m);
    maps.set(id, m);
    return true;
  };
  w.mapSetView = function (id, lat, lng, zoom) { const m = maps.get(id); if (m) m.setView([lat, lng], zoom); };
  w.mapPanBy = function (id, dx, dy) { const m = maps.get(id); if (m) m.panBy([dx, dy], { animate: false }); };
  w.mapDestroy = function (id) { const m = maps.get(id); if (m) { m.remove(); maps.delete(id); } };
  w.mapAddPin = function (id, lat, lng, color, label) {
    const m = maps.get(id); if (!m) return null;
    const html = '<div style="width:30px;height:30px;border-radius:50%;background:' + (color || '#9a3a10') +
                 ';border:3px solid #ffffff;box-shadow:0 0 0 1px #cbd5e1;display:flex;align-items:center;justify-content:center;color:#fff;font-weight:800">' + (label || '') + '</div>';
    return L.marker([lat, lng], { icon: L.divIcon({ className: '', html, iconSize: [30, 30], iconAnchor: [15, 15] }) }).addTo(m);
  };
  w.mapDrawRoute = function (id, coords, color) {
    const m = maps.get(id); if (!m || !coords || coords.length < 2) return;
    const line = L.polyline(coords, { color: color || '#3b82f6', weight: 6, opacity: .9 }).addTo(m);
    try { m.fitBounds(line.getBounds(), { padding: [40, 40] }); } catch (_) {}
    return line;
  };

  // ─── IndexedDB-backed wolfs_* store with Cloudflare R2 sync ──────────
  // Mirrors the existing /wolfstruckingco.com/db.js so existing R2 records keep working.
  function dbOpen() {
    return new Promise((res, rej) => {
      const r = indexedDB.open('wolfs', 1);
      r.onupgradeneeded = () => {
        const d = r.result;
        ['users','workers','jobs','timesheets','applicants','listings','purchases','badges','roles','customers','audit'].forEach(s => {
          if (!d.objectStoreNames.contains(s)) d.createObjectStore(s, { keyPath: 'id' });
        });
      };
      r.onsuccess = () => res(r.result);
      r.onerror = () => rej(r.error);
    });
  }
  async function dbAll(store) {
    const db = await dbOpen();
    return await new Promise((res, rej) => {
      const tx = db.transaction(store, 'readonly');
      const req = tx.objectStore(store).getAll();
      req.onsuccess = () => res(req.result || []);
      req.onerror = () => rej(req.error);
    });
  }
  async function dbPut(store, val) {
    const db = await dbOpen();
    return await new Promise((res, rej) => {
      const tx = db.transaction(store, 'readwrite');
      tx.objectStore(store).put(val);
      tx.oncomplete = () => res(true);
      tx.onerror = () => rej(tx.error);
    });
  }
  async function dbGet(store, id) {
    const db = await dbOpen();
    return await new Promise((res, rej) => {
      const tx = db.transaction(store, 'readonly');
      const req = tx.objectStore(store).get(id);
      req.onsuccess = () => res(req.result || null);
      req.onerror = () => rej(req.error);
    });
  }
  w.dbAll = dbAll; w.dbPut = dbPut; w.dbGet = dbGet;
  w.dbAllJson = async function (store) { return JSON.stringify(await dbAll(store)); };

  // ─── Cloudflare worker calls (sign in / sign up / chat / loc / buy) ──
  w.workerPost = async function (path, body, headers) {
    const r = await fetch(WORKER + path, {
      method: 'POST',
      headers: Object.assign({ 'Content-Type': 'application/json' }, headers || {}),
      body: typeof body === 'string' ? body : JSON.stringify(body || {}),
    });
    const txt = await r.text();
    return { ok: r.ok, status: r.status, body: txt };
  };
  w.workerGet = async function (path) {
    const r = await fetch(WORKER + path);
    return { ok: r.ok, status: r.status, body: await r.text() };
  };

  // ─── Theme ────────────────────────────────────────────────────────────
  w.themeRead = function () { return localStorage.getItem('wolfs_theme') || 'auto'; };
  w.themeWrite = function (v) { try { localStorage.setItem('wolfs_theme', v); document.documentElement.setAttribute('data-theme', v === 'auto' ? '' : v); } catch (_) {} };
  w.themeResolved = function () {
    const t = w.themeRead();
    if (t === 'auto') return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    return t;
  };

  // ─── Auth (localStorage-backed, same keys as the static site) ────────
  w.authGet = function () {
    return {
      role: (localStorage.getItem('wolfs_role') || '').toLowerCase() || null,
      email: localStorage.getItem('wolfs_email') || null,
      sess:  localStorage.getItem('wolfs_session') || null,
    };
  };
  w.authSet = function (role, email, sess) {
    if (role) localStorage.setItem('wolfs_role', role); else localStorage.removeItem('wolfs_role');
    if (email) localStorage.setItem('wolfs_email', email); else localStorage.removeItem('wolfs_email');
    if (sess) localStorage.setItem('wolfs_session', sess); else localStorage.removeItem('wolfs_session');
  };
  w.authClear = function () { ['wolfs_role','wolfs_email','wolfs_session'].forEach(k => localStorage.removeItem(k)); };

  window.WolfsInterop = w;
})();
