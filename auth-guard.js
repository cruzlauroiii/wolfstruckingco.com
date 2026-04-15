// Role-based page guard.
// Pages that only certain roles should see declare their allowed roles via
//   <script>window.__allowedRoles = ['admin','staff']</script>
// BEFORE including this script. If the signed-in role is not in that list,
// we redirect to the role's own home page. If the user is not signed in at all
// we send them to /Login/.
(function(){
  var roleHome = {
    applicant: '/wolfstruckingco.com/Applicant/',
    driver:    '/wolfstruckingco.com/Dashboard/',
    client:    '/wolfstruckingco.com/Employer/',
    employer:  '/wolfstruckingco.com/Employer/',
    admin:     '/wolfstruckingco.com/HiringHall/',
    staff:     '/wolfstruckingco.com/HiringHall/',
    investor:  '/wolfstruckingco.com/Investors/KPI/',
  };
  var allowed = window.__allowedRoles;
  if (!allowed || !allowed.length) return;
  var sess = localStorage.getItem('wolfs_session');
  if (!sess) {
    window.location.replace('/wolfstruckingco.com/Login/');
    return;
  }
  var role = (localStorage.getItem('wolfs_role') || '').toLowerCase();
  if (allowed.indexOf(role) === -1) {
    var dest = roleHome[role] || '/wolfstruckingco.com/Login/';
    window.location.replace(dest);
  }
})();
