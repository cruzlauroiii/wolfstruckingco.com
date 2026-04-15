// WolfsDB - Cloudflare R2-backed data layer via Worker REST API.
// Falls back to local IndexedDB only when the network is unreachable.
// No auto-seed — records are created via explicit CRUD by the signed-in user.

(function(){
  const API = 'https://wolfstruckingco.nbth.workers.dev/api';
  const DB_NAME = 'WolfsDB';
  const DB_VERSION = 1;
  const STORES = ['workers','badges','roles','customers','schedules','timesheets','chatSessions','jobs','jobMatches','agentProfiles','kv'];

  let dbPromise = null;
  function openLocal() {
    if (dbPromise) return dbPromise;
    dbPromise = new Promise((resolve, reject) => {
      const req = indexedDB.open(DB_NAME, DB_VERSION);
      req.onupgradeneeded = (ev) => {
        const db = ev.target.result;
        for (const name of STORES) {
          if (!db.objectStoreNames.contains(name)) db.createObjectStore(name, { keyPath: name === 'agentProfiles' ? 'workerId' : name === 'kv' ? 'k' : 'id' });
        }
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
    return dbPromise;
  }

  function authHeaders() {
    const sess = localStorage.getItem('wolfs_session');
    const email = localStorage.getItem('wolfs_email');
    const role = localStorage.getItem('wolfs_role');
    const h = {};
    if (sess) h['X-Wolfs-Session'] = sess;
    if (email) h['X-Wolfs-Email'] = email;
    if (role) h['X-Wolfs-Role'] = role;
    return h;
  }

  async function remoteGetAll(store) {
    const res = await fetch(`${API}/${store}`, { headers: authHeaders() });
    if (!res.ok) throw new Error(`GET ${store}: ${res.status}`);
    const data = await res.json();
    return data.items || [];
  }
  async function remoteGet(store, id) {
    const res = await fetch(`${API}/${store}/${encodeURIComponent(id)}`, { headers: authHeaders() });
    if (res.status === 404) return undefined;
    if (!res.ok) throw new Error(`GET ${store}/${id}: ${res.status}`);
    return res.json();
  }
  async function remotePut(store, record) {
    const existingRes = record.id ? await fetch(`${API}/${store}/${encodeURIComponent(record.id)}`, { headers: authHeaders() }) : null;
    const method = existingRes && existingRes.ok ? 'PUT' : 'POST';
    const url = method === 'PUT' ? `${API}/${store}/${encodeURIComponent(record.id)}` : `${API}/${store}`;
    const res = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json', ...authHeaders() },
      body: JSON.stringify(record),
    });
    if (!res.ok) throw new Error(`${method} ${store}: ${res.status}`);
    const out = await res.json();
    return out.item || record;
  }
  async function remoteDel(store, id) {
    const res = await fetch(`${API}/${store}/${encodeURIComponent(id)}`, { method: 'DELETE', headers: authHeaders() });
    if (!res.ok && res.status !== 404) throw new Error(`DELETE ${store}/${id}: ${res.status}`);
  }

  // Local-IDB fallback helpers
  async function tx(store, mode) { const db = await openLocal(); return db.transaction(store, mode).objectStore(store); }
  async function localPut(store, record) { const s = await tx(store, 'readwrite'); return new Promise((res, rej) => { const r = s.put(record); r.onsuccess = () => res(record); r.onerror = () => rej(r.error); }); }
  async function localGet(store, id) { const s = await tx(store, 'readonly'); return new Promise((res, rej) => { const r = s.get(id); r.onsuccess = () => res(r.result); r.onerror = () => rej(r.error); }); }
  async function localDel(store, id) { const s = await tx(store, 'readwrite'); return new Promise((res, rej) => { const r = s.delete(id); r.onsuccess = () => res(); r.onerror = () => rej(r.error); }); }
  async function localAll(store) { const s = await tx(store, 'readonly'); return new Promise((res, rej) => { const r = s.getAll(); r.onsuccess = () => res(r.result || []); r.onerror = () => rej(r.error); }); }

  let forceLocal = false;
  function shouldUseRemote() {
    if (forceLocal) return false;
    try { return navigator.onLine !== false; } catch { return true; }
  }

  async function put(store, record) {
    if (shouldUseRemote()) {
      try { return await remotePut(store, record); }
      catch (e) { console.warn('[WolfsDB] remote put failed, falling back', e); forceLocal = true; }
    }
    return localPut(store, record);
  }
  async function get(store, id) {
    if (shouldUseRemote()) {
      try { return await remoteGet(store, id); }
      catch (e) { console.warn('[WolfsDB] remote get failed, falling back', e); forceLocal = true; }
    }
    return localGet(store, id);
  }
  async function all(store) {
    if (shouldUseRemote()) {
      try { return await remoteGetAll(store); }
      catch (e) { console.warn('[WolfsDB] remote all failed, falling back', e); forceLocal = true; }
    }
    return localAll(store);
  }
  async function del(store, id) {
    if (shouldUseRemote()) {
      try { return await remoteDel(store, id); }
      catch (e) { console.warn('[WolfsDB] remote del failed, falling back', e); forceLocal = true; }
    }
    return localDel(store, id);
  }

  async function kvGet(k) { const r = await get('kv', k); return r ? r.v : undefined; }
  async function kvSet(k, v) { return put('kv', { k, v }); }

  function uuid(prefix) { return (prefix || '') + Math.random().toString(36).slice(2, 10) + Date.now().toString(36); }

  window.WolfsDB = {
    API, put, get, del, all, kvGet, kvSet, uuid,
    STORES,
    // seed() is a no-op here (R2 backend, user-driven CRUD). Kept for API compat.
    seed: async () => { },
    reseed: async () => { },
  };
})();
