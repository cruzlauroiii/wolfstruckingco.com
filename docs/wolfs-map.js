// Shared tile-layer theming. Wraps L.tileLayer so any URL with cartocdn `dark_all` or
// `light_all` automatically swaps when the user toggles dark/light via theme.js.
//
//   <script src="/wolfstruckingco.com/wolfs-map.js"></script>
//
// Pages keep their existing L.tileLayer(...) call — this file replaces L.tileLayer with
// a wrapped version that registers the layer for theme-driven URL swaps. Listens for the
// `wolfs:theme` CustomEvent that theme.js dispatches with `detail.resolved` ('dark'|'light').

(function () {
  if (typeof window === 'undefined' || !window.L || window.__WolfsMapWrapped) return;
  window.__WolfsMapWrapped = true;

  var tracked = [];
  var origTileLayer = window.L.tileLayer;

  function resolvedTheme() {
    var t = (localStorage.getItem('wolfs_theme') || 'auto');
    if (t === 'auto') {
      try { return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'; }
      catch (_) { return 'dark'; }
    }
    return t === 'light' ? 'light' : 'dark';
  }

  function urlForTheme(originalUrl, theme) {
    if (!originalUrl) return originalUrl;
    if (theme === 'light') {
      return originalUrl
        .replace('/dark_all/', '/light_all/')
        .replace('/dark_nolabels/', '/light_nolabels/');
    }
    return originalUrl
      .replace('/light_all/', '/dark_all/')
      .replace('/light_nolabels/', '/dark_nolabels/');
  }

  window.L.tileLayer = function wrappedTileLayer(url, options) {
    var layer = origTileLayer.call(this, url, options);
    if (typeof url === 'string' && /basemaps\.cartocdn\.com/.test(url)) {
      var entry = { layer: layer, baseUrl: url };
      tracked.push(entry);
      // Apply current theme immediately if it differs from the URL given.
      var t = resolvedTheme();
      var swapped = urlForTheme(url, t);
      if (swapped !== url && layer.setUrl) { try { layer.setUrl(swapped); } catch (_) {} }
    }
    return layer;
  };
  // Preserve static methods/proto.
  for (var k in origTileLayer) { if (Object.prototype.hasOwnProperty.call(origTileLayer, k)) window.L.tileLayer[k] = origTileLayer[k]; }

  function applyTheme(t) {
    for (var i = 0; i < tracked.length; i++) {
      var e = tracked[i];
      var u = urlForTheme(e.baseUrl, t);
      if (e.layer && e.layer.setUrl) { try { e.layer.setUrl(u); } catch (_) {} }
    }
  }

  window.addEventListener('wolfs:theme', function (ev) {
    var t = ev && ev.detail && ev.detail.resolved ? ev.detail.resolved : resolvedTheme();
    applyTheme(t === 'light' ? 'light' : 'dark');
  });

  // OS-level dark/light flip when user is on `auto`.
  try {
    var mq = window.matchMedia('(prefers-color-scheme: dark)');
    mq.addEventListener && mq.addEventListener('change', function () {
      if ((localStorage.getItem('wolfs_theme') || 'auto') === 'auto') applyTheme(resolvedTheme());
    });
  } catch (_) {}
})();
