// Shared theme toggle — drop it into every page and a theme chip appears in the header.
//
//   <script src="/wolfstruckingco.com/theme.js"></script>
//
// The button cycles dark → light → auto (follows OS). Selection persists in localStorage
// under `wolfs_theme`. Consumers can react to theme changes by listening for the
// `wolfs:theme` CustomEvent on window, which fires with { detail: { theme, resolved } }.

(function () {
  'use strict';

  const KEY = 'wolfs_theme';
  const ICONS = { dark: '🌙', light: '☀️', auto: '🌗' };
  const LABELS = { dark: 'Dark', light: 'Light', auto: 'Auto' };
  const NEXT  = { dark: 'light', light: 'auto', auto: 'dark' };

  function read() {
    try { return localStorage.getItem(KEY) || 'dark'; } catch (_) { return 'dark'; }
  }
  function write(v) {
    try { localStorage.setItem(KEY, v); } catch (_) {}
  }
  function systemPrefersLight() {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches;
  }
  function resolve(choice) {
    if (choice === 'auto') return systemPrefersLight() ? 'light' : 'dark';
    return choice;
  }
  function apply(choice) {
    const resolved = resolve(choice);
    document.documentElement.setAttribute('data-theme', resolved);
    try { window.dispatchEvent(new CustomEvent('wolfs:theme', { detail: { theme: choice, resolved } })); } catch (_) {}
    refreshChipLabel();
  }
  function refreshChipLabel() {
    const btn = document.getElementById('WolfsThemeChip');
    if (!btn) return;
    const cur = read();
    btn.textContent = ICONS[cur] + ' ' + LABELS[cur];
    btn.title = 'Theme: ' + LABELS[cur] + ' — click to change';
  }
  function cycle() {
    const cur = read();
    const next = NEXT[cur] || 'dark';
    write(next);
    apply(next);
  }

  // ─── Inject stylesheet for light theme tokens ────────────────────────────
  function ensureStyles() {
    if (document.getElementById('WolfsThemeStyles')) return;
    const s = document.createElement('style');
    s.id = 'WolfsThemeStyles';
    s.textContent = `
:root[data-theme="light"]{
  --bg:#f4f6fa;
  --card:#ffffff;
  --border:#d4dae3;
  --text:#0f1419;
  --text-muted:#5b6a7a;
  --accent:#e95a1c;
  --accent-hover:#d84c10;
  --success:#16a34a;
  --info:#2563eb;
  --warning:#b45309;
  --danger:#dc2626;
}
:root[data-theme="light"] body,:root[data-theme="light"] html{background:var(--bg);color:var(--text)}
:root[data-theme="light"] h1,:root[data-theme="light"] h2,:root[data-theme="light"] h3,:root[data-theme="light"] h4,:root[data-theme="light"] p,:root[data-theme="light"] li,:root[data-theme="light"] span,:root[data-theme="light"] div,:root[data-theme="light"] td,:root[data-theme="light"] th,:root[data-theme="light"] label{color:inherit}
:root[data-theme="light"] .TopBar:not(.TopBarOverlay){background:rgba(255,255,255,.92)!important;border-color:var(--border)!important;color:var(--text)!important}
/* Dashboard / map-overlay pages: keep TopBar transparent in light mode. */
:root[data-theme="light"] .TopBar.TopBarOverlay{background:transparent!important;border:none!important}
:root[data-theme="light"] .TopBar a,:root[data-theme="light"] .TopActions a{color:var(--text-muted)!important}
:root[data-theme="light"] .TopBar a:hover,:root[data-theme="light"] .TopActions a:hover{color:var(--accent)!important}
:root[data-theme="light"] .Brand,:root[data-theme="light"] .Brand a{color:var(--text)!important}
:root[data-theme="light"] .Card,:root[data-theme="light"] .WChat,:root[data-theme="light"] .Panel{background:var(--card)!important;color:var(--text)!important;border-color:var(--border)!important}
:root[data-theme="light"] .Card *:not(input):not(textarea):not(select):not(button):not(.Btn):not(.Pill):not(.Msg):not(.Msg.User):not(.WMsg):not(.WMsg.User):not(.Label):not(.WLabel){color:inherit}
:root[data-theme="light"] .Msg.User,:root[data-theme="light"] .WMsg.User{background:var(--accent)!important;color:#fff!important}
:root[data-theme="light"] .Msg.Agent,:root[data-theme="light"] .WMsg.Agent{background:rgba(233,90,28,.06)!important;color:var(--text)!important}
/* Default label is orange; on user bubbles that IS the background, so flip to white. */
:root[data-theme="light"] .Label,:root[data-theme="light"] .WLabel{color:var(--accent)!important}
:root[data-theme="light"] .Msg.User .Label,:root[data-theme="light"] .WMsg.User .WLabel{color:#fff!important;opacity:.9}
:root[data-theme="light"] .Msg.User *,:root[data-theme="light"] .WMsg.User *{color:#fff!important}
:root[data-theme="light"] input,:root[data-theme="light"] textarea,:root[data-theme="light"] select{background:#fff!important;color:var(--text)!important;border-color:var(--border)!important}
:root[data-theme="light"] input::placeholder,:root[data-theme="light"] textarea::placeholder{color:var(--text-muted)!important}
:root[data-theme="light"] a:not(.Btn):not(.NavCta){color:var(--accent)}
#WolfsThemeChip{padding:6px 10px;border-radius:999px;border:1px solid var(--border,#2a3a4a);background:transparent;color:var(--text-muted,#8899aa);font-size:.8rem;font-family:inherit;cursor:pointer;display:inline-flex;align-items:center;gap:4px;white-space:nowrap}
#WolfsThemeChip:hover{color:var(--accent,#ff6b35);border-color:var(--accent,#ff6b35)}
`;
    document.head.appendChild(s);
  }

  // ─── Inject the theme chip into whichever header container is available ─
  function mountChip() {
    if (document.getElementById('WolfsThemeChip')) return;
    const btn = document.createElement('button');
    btn.id = 'WolfsThemeChip';
    btn.type = 'button';
    btn.addEventListener('click', cycle);
    // Prefer nav-style containers (chip should be inline with other nav items there).
    // For .TopBar (used on Dashboard / map-style layouts) we APPEND so we don't displace
    // the burger button via flex `justify-content:space-between`.
    const containers = [
      { sel: '.NavPinned',  prepend: true },
      { sel: '.TopActions', prepend: true },
      { sel: '.NavLinks',   prepend: true },
      { sel: '.TopBar',     prepend: false },
      { sel: '.Header',     prepend: true },
    ];
    for (const c of containers) {
      const el = document.querySelector(c.sel);
      if (el) {
        if (c.prepend) el.insertBefore(btn, el.firstChild);
        else el.appendChild(btn);
        refreshChipLabel();
        return;
      }
    }
    // Final fallback — pin to top-right of the viewport so it's always reachable.
    btn.style.cssText = 'position:fixed;top:14px;right:14px;z-index:9999;' + btn.style.cssText;
    document.body.appendChild(btn);
    refreshChipLabel();
  }

  // Start.
  ensureStyles();
  apply(read());
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', mountChip);
  } else {
    mountChip();
  }
  // Re-apply "auto" if the OS preference changes.
  if (window.matchMedia) {
    try { window.matchMedia('(prefers-color-scheme: light)').addEventListener('change', () => { if (read() === 'auto') apply('auto'); }); } catch (_) {}
  }
})();
