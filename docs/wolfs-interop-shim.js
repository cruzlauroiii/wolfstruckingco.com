// wolfs-interop-shim.js - exposes window.WolfsInterop on standalone pages so
// the same scene scripts (dbPut/dbAll/authSet/authClear) work without Blazor.
(function () {
  const ROLE_KEY = 'wolfs_role';
  const SESS_KEY = 'wolfs_session';
  const EMAIL_KEY = 'wolfs_email';

  window.WolfsInterop = {
    async dbPut(store, record) {
      if (!window.WolfsDB) { throw new Error('WolfsDB not loaded'); }
      return window.WolfsDB.put(store, record);
    },
    async dbAll(store) {
      if (!window.WolfsDB) { throw new Error('WolfsDB not loaded'); }
      return window.WolfsDB.all(store);
    },
    async dbGet(store, id) {
      if (!window.WolfsDB) { throw new Error('WolfsDB not loaded'); }
      return window.WolfsDB.get(store, id);
    },
    async dbDel(store, id) {
      if (!window.WolfsDB) { throw new Error('WolfsDB not loaded'); }
      return window.WolfsDB.del(store, id);
    },
    authSet(role, email, sessionToken) {
      try {
        localStorage.setItem(ROLE_KEY, role || '');
        localStorage.setItem(EMAIL_KEY, email || '');
        localStorage.setItem(SESS_KEY, sessionToken || '');
      } catch (_) { /* storage may be blocked */ }
    },
    authClear() {
      try {
        localStorage.removeItem(ROLE_KEY);
        localStorage.removeItem(EMAIL_KEY);
        localStorage.removeItem(SESS_KEY);
      } catch (_) { /* storage may be blocked */ }
    },
  };
})();
