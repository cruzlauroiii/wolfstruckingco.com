// Role-based page guard.
//
// Pages that only certain roles should see declare their allowed roles via
//   <script>window.__allowedRoles = ['admin','driver']</script>
// BEFORE including this script. The role model has three values: admin, driver, user.
// Legacy role values (applicant, staff, employer, client, investor) from older sessions are
// folded to one of the three so we don't kick existing accounts out.
//
// If the signed-in role is not in the page's allow-list, we redirect to that role's home.
// If the user is not signed in at all we send them to /Login/.
(function(){
  var legacyMap = { applicant:'user', staff:'admin', employer:'user', client:'user', investor:'user' };
  var roleHome = {
    admin:  '/wolfstruckingco.com/HiringHall/',
    driver: '/wolfstruckingco.com/Dashboard/',
    user:   '/wolfstruckingco.com/Marketplace/',
  };
  var allowed = window.__allowedRoles;
  if (!allowed || !allowed.length) return;
  var sess = localStorage.getItem('wolfs_session');
  if (!sess) {
    window.location.replace('/wolfstruckingco.com/Login/');
    return;
  }
  var raw = (localStorage.getItem('wolfs_role') || '').toLowerCase();
  var role = roleHome[raw] ? raw : (legacyMap[raw] || 'user');
  // Persist the normalized role so other components see only admin/driver/user from now on.
  if (raw && raw !== role) {
    try { localStorage.setItem('wolfs_role', role); } catch (_) {}
  }
  if (allowed.indexOf(role) === -1) {
    var dest = roleHome[role] || '/wolfstruckingco.com/Login/';
    window.location.replace(dest);
  }
})();
