// Shared auth-aware header affordance.
//
// When a user is signed in we either:
//   (a) rewrite the existing "Sign In" / "Switch user" link in the page chrome,
//       turning it into "<Name> · <role> · Sign out"; or
//   (b) if the page has no such link, we append a plain text Sign out link into
//       the first nav-ish container we find (`.TopActions`, `.NavPinned`, or the
//       element adjacent to the brand). Never use a floating overlay — the user
//       asked for text in the header, not a pill that can cover other text.
(function(){
  function cleanDisplayName() {
    var n = (localStorage.getItem('wolfs_name') || '').trim();
    if (n && n !== 'undefined') return n;
    var e = (localStorage.getItem('wolfs_email') || '').trim();
    if (e) return e.split('@')[0];
    return localStorage.getItem('wolfs_role') || 'User';
  }
  function signOut() {
    localStorage.removeItem('wolfs_session');
    localStorage.removeItem('wolfs_email');
    localStorage.removeItem('wolfs_role');
    localStorage.removeItem('wolfs_name');
    localStorage.removeItem('wolfs_demo');
    window.location.href = '/wolfstruckingco.com/Login/';
  }
  window.__wolfsSignOut = signOut;

  function buildSignOutAnchor(label) {
    var a = document.createElement('a');
    a.href = '#signout';
    a.textContent = label;
    a.style.cssText = 'color:var(--text-muted,#8899aa);text-decoration:none;padding:8px 12px;font-size:.88rem';
    a.addEventListener('click', function(ev){ ev.preventDefault(); signOut(); });
    a.addEventListener('mouseover', function(){ a.style.color = 'var(--accent,#ff6b35)'; });
    a.addEventListener('mouseout', function(){ a.style.color = 'var(--text-muted,#8899aa)'; });
    return a;
  }

  function apply() {
    var sess = localStorage.getItem('wolfs_session');
    if (!sess) return;
    var name = cleanDisplayName();
    var role = (localStorage.getItem('wolfs_role') || '').toLowerCase();
    var label = name + (role ? ' · ' + role : '') + '  ·  Sign out';

    // Strategy (a): rewrite any existing /Login anchor into a sign-out affordance.
    var links = document.querySelectorAll('a[href*="/Login"]');
    if (links.length) {
      for (var i = 0; i < links.length; i++) {
        var a = links[i];
        a.textContent = label;
        a.setAttribute('href', '#signout');
        a.setAttribute('title', 'Sign out');
        a.addEventListener('click', function(ev){ ev.preventDefault(); signOut(); });
      }
      return;
    }

    // Strategy (b): no existing link — append a plain text link into the header.
    var candidateContainers = ['.TopActions', '.NavPinned', '.HeaderRight', '.TopBar', '.Header'];
    for (var j = 0; j < candidateContainers.length; j++) {
      var box = document.querySelector(candidateContainers[j]);
      if (box) {
        box.appendChild(buildSignOutAnchor(label));
        return;
      }
    }
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', apply);
  else apply();
})();
