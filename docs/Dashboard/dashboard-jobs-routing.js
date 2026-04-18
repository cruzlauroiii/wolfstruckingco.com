// Render real employer-posted jobs from R2 that match this driver's badges.
async function RenderRealJobs() {
  try {
    var email = (localStorage.getItem('wolfs_email') || '').toLowerCase();
    var [workers, jobs] = await Promise.all([WolfsDB.all('workers'), WolfsDB.all('jobs')]);
    var me = workers.find(function(w) { return (w.email || '').toLowerCase() === email; });
    var el = document.getElementById('RealJobList');
    if (!el) return;
    if (!me) { el.innerHTML = '<p style="color:var(--text-muted);padding:10px">Sign in as an approved driver to see jobs that match your badges.</p>'; return; }
    var myBadges = me.badges || [];
    var open = jobs.filter(function(j) { return j.status === 'open' && (j.requiredBadges || []).every(function(b) { return myBadges.indexOf(b) !== -1; }); });
    var cnt = document.getElementById('JobCount'); if (cnt) cnt.textContent = open.length + ' Available';
    if (!open.length) { el.innerHTML = '<p style="color:var(--text-muted);padding:10px;font-size:.85rem">No employer-posted jobs matching your badges right now.</p>'; return; }
    el.innerHTML = '<div style="font-size:.7rem;text-transform:uppercase;letter-spacing:.4px;color:var(--text-muted);margin-bottom:8px">Employer-posted — matches your badges</div>' + open.map(function(j) {
      var pay = (j.payRate || 0) * (j.hours || 0);
      var stopsLine = (j.stops && j.stops.length)
        ? '<div style="color:var(--info);font-size:.78rem;margin-bottom:4px">📍 ' + j.stops.length + ' intermediate stop' + (j.stops.length===1?'':'s') + ': ' + j.stops.map(function(s){return s;}).join(' → ') + '</div>'
        : '';
      return '<div class="JobRow" data-job-id="' + j.id + '" style="padding:12px;border:1px solid var(--border);border-radius:8px;margin-bottom:10px">' +
        '<div style="font-weight:700;margin-bottom:4px">' + (j.title || 'Job') + '</div>' +
        '<div style="color:var(--success);font-size:.85rem;margin-bottom:3px"><b>FROM</b> ' + (j.pickup||'—') + '</div>' +
        stopsLine +
        '<div style="color:var(--accent);font-size:.85rem;margin-bottom:6px"><b>TO</b> ' + (j.delivery||'—') + '</div>' +
        '<div style="color:var(--text-muted);font-size:.82rem;margin-bottom:8px">' + (j.hours||0) + 'h @ $' + (j.payRate||0) + '/hr · <b style="color:var(--success)">$' + pay.toFixed(0) + ' total</b></div>' +
        '<div style="display:flex;gap:8px;flex-wrap:wrap">' +
          '<button onclick="PreviewJobRoute(\'' + j.id + '\')" style="background:transparent;color:var(--info);border:1px solid var(--info);padding:6px 14px;border-radius:6px;cursor:pointer;font-weight:700;font-size:.82rem">🗺️ See route</button>' +
          (j.status === 'accepted' || j.status === 'in_progress' ? '<button onclick="StartTurnByTurn(\'' + j.id + '\')" style="background:#3b82f6;color:#fff;border:none;padding:6px 14px;border-radius:6px;cursor:pointer;font-weight:700;font-size:.82rem">🔊 Navigate with voice</button>' : '') +
          '<button onclick="AcceptRealJob(\'' + j.id + '\')" style="background:#22c55e;color:#fff;border:none;padding:6px 14px;border-radius:6px;cursor:pointer;font-weight:700;font-size:.82rem">Accept</button>' +
        '</div>' +
        '<div class="JobRouteMap" id="JobRouteMap_' + j.id + '" style="display:none;width:100%;height:240px;border-radius:8px;border:1px solid var(--border);background:#0b1016;margin-top:10px"></div>' +
        '<div class="JobRouteInfo" id="JobRouteInfo_' + j.id + '" style="display:none;font-size:.8rem;color:var(--text-muted);margin-top:6px"></div>' +
      '</div>';
    }).join('');
    window.__driverOpenJobs = open;
  } catch (ex) { console.error(ex); }
}
window.__driverRouteMaps = {};
window.__driverGeocodeCache = {};
// Pre-baked coordinates for common demo addresses so the route always renders even when
// Nominatim is rate-limited or unreachable. Nominatim's free tier is 1 req/sec and
// often blocks requests without a proper User-Agent — we fall back to this table on miss.
window.__driverGeocodeStatic = window.__driverGeocodeStatic || {
  'port of los angeles': [33.7395, -118.2730],
  'port of la': [33.7395, -118.2730],
  'long beach': [33.7701, -118.1937],
  'port of long beach': [33.7475, -118.2147],
  'wilmington, nc': [34.2257, -77.9447],
  'wilmington nc': [34.2257, -77.9447],
  'wilmington, ca': [33.7895, -118.2664],
  'los angeles, ca': [34.0522, -118.2437],
  'los angeles': [34.0522, -118.2437],
  'la': [34.0522, -118.2437],
  '1200 wilshire blvd, los angeles, ca': [34.0511, -118.2683],
  '450 n roxbury dr, beverly hills, ca': [34.0721, -118.4053],
  'beverly hills, ca': [34.0736, -118.4004],
  'whole foods — 7871 beverly blvd, la': [34.0763, -118.3503],
  'whole foods 7871 beverly blvd, la': [34.0763, -118.3503],
  '7871 beverly blvd, la': [34.0763, -118.3503],
};
function __drvNormalizeAddr(s){ return (s||'').toLowerCase().trim().replace(/\s+/g,' '); }
async function __drvGeocode(q){
  if(!q||!q.trim())return null;
  const key=__drvNormalizeAddr(q);
  if(window.__driverGeocodeCache[q])return window.__driverGeocodeCache[q];
  if(window.__driverGeocodeStatic[key]){ const ll=window.__driverGeocodeStatic[key]; window.__driverGeocodeCache[q]=ll; return ll; }
  // Fuzzy match: any pre-baked key contained in the query (e.g. "Port of LA, CA, USA" → port of la).
  for(const k in window.__driverGeocodeStatic){ if(key.indexOf(k)!==-1 || k.indexOf(key)!==-1){ const ll=window.__driverGeocodeStatic[k]; window.__driverGeocodeCache[q]=ll; return ll; } }
  try {
    const r=await fetch('https://nominatim.openstreetmap.org/search?format=json&limit=1&q='+encodeURIComponent(q),
      { headers: { 'Accept': 'application/json' } });
    const a=await r.json();
    if(a&&a.length){const ll=[parseFloat(a[0].lat),parseFloat(a[0].lon)]; window.__driverGeocodeCache[q]=ll; return ll;}
  } catch(_){}
  return null;
}
function __drvTraffic(km,min){ if(!km||!min)return{label:'unknown',color:'#8899aa'}; const k=(km/min)*60; if(k>=55)return{label:'clear',color:'#22c55e'}; if(k>=30)return{label:'moderate',color:'#f59e0b'}; return{label:'heavy',color:'#ef4444'}; }
async function PreviewJobRoute(jobId){
  var mapEl=document.getElementById('JobRouteMap_'+jobId);
  var infoEl=document.getElementById('JobRouteInfo_'+jobId);
  if(!mapEl)return;
  if(mapEl.style.display==='block'){ mapEl.style.display='none'; infoEl.style.display='none'; return; }
  mapEl.style.display='block'; infoEl.style.display='block'; infoEl.textContent='Loading route…';
  var j=(window.__driverOpenJobs||[]).find(function(x){return x.id===jobId;});
  if(!j){ infoEl.textContent='Job not found.'; return; }
  var m = window.__driverRouteMaps[jobId] || (window.__driverRouteMaps[jobId] = L.map(mapEl,{zoomControl:true,attributionControl:false}).setView([34.05,-118.24],10));
  L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',{maxZoom:19,subdomains:'abcd'}).addTo(m);
  m.invalidateSize();
  var addrs=[j.pickup, ...(j.stops||[]), j.delivery];
  var coords=[];
  for(var a of addrs){ var c=await __drvGeocode(a); if(c) coords.push(c); }
  if(coords.length<2){ infoEl.textContent='Could not geocode this route.'; return; }
  var waypoints=coords.map(function(p){return L.latLng(p[0],p[1]);});
  if(m._rc) m.removeControl(m._rc);
  m._rc = L.Routing.control({
    waypoints, router: L.Routing.osrmv1({serviceUrl:'https://router.project-osrm.org/route/v1'}),
    lineOptions:{styles:[{color:'#22c55e',weight:5,opacity:.9}]},
    show:false, addWaypoints:false, draggableWaypoints:false, fitSelectedRoutes:true,
    createMarker:function(i,wp){ return L.marker(wp.latLng,{icon:L.divIcon({className:'',html:'<div style="width:22px;height:22px;border-radius:50%;background:'+(i===0?'#22c55e':i===waypoints.length-1?'#ff6b35':'#3b82f6')+';border:3px solid #0f1419;display:flex;align-items:center;justify-content:center;color:#fff;font-size:.65rem;font-weight:800">'+(i===0?'P':i===waypoints.length-1?'D':i)+'</div>',iconSize:[22,22],iconAnchor:[11,11]})}); }
  }).on('routesfound',function(e){
    var r=e.routes[0]; var km=r.summary.totalDistance/1000; var min=r.summary.totalTime/60; var t=__drvTraffic(km,min);
    infoEl.innerHTML='<b>'+km.toFixed(1)+' km</b> · <b>'+Math.round(min)+' min</b> · <span style="color:'+t.color+';font-weight:700;text-transform:uppercase">'+t.label+' traffic</span> · <b>'+coords.length+' waypoints</b> (this is what you\'ll drive)';
  }).on('routingerror',function(){ infoEl.textContent='Routing error — OSRM may be rate-limited. Try again.'; }).addTo(m);
}
window.PreviewJobRoute = PreviewJobRoute;

// ─── Turn-by-turn voice navigation ───────────────────────────────────
// Wraps WolfsNav.start: re-uses the JobRouteMap so the driver doesn't lose context, swaps
// in a Valhalla-routed line, pre-fetches voice clips per maneuver, and announces each turn
// as the truck approaches the maneuver point.
async function StartTurnByTurn(jobId) {
  if (!window.WolfsNav) { alert('Navigation module not loaded.'); return; }
  await PreviewJobRoute(jobId);
  const job = (window.__driverOpenJobs || []).find(x => x.id === jobId)
            || (await WolfsDB.all('jobs')).find(x => x.id === jobId);
  if (!job) { alert('Job not found.'); return; }
  const mapEl = document.getElementById('JobRouteMap_' + jobId);
  const m = window.__driverRouteMaps[jobId];
  if (!m || !mapEl) { alert('Open See route first.'); return; }
  // Reuse the geocoded waypoints we already cached during PreviewJobRoute.
  const addrs = [job.pickup, ...(job.stops || []), job.delivery];
  const coords = [];
  for (const a of addrs) { const c = await __drvGeocode(a); if (c) coords.push(c); }
  if (coords.length < 2) { alert('Could not geocode the route.'); return; }
  const origin = coords[0];
  const destination = coords[coords.length - 1];
  const stops = coords.slice(1, -1);
  // Big nav status banner above the map.
  let banner = document.getElementById('NavBanner_' + jobId);
  if (!banner) {
    banner = document.createElement('div');
    banner.id = 'NavBanner_' + jobId;
    banner.style.cssText = 'background:linear-gradient(135deg,#3b82f6,#2563eb);color:#fff;padding:18px 22px;border-radius:10px;font-weight:800;font-size:1.15rem;margin-bottom:10px;display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;box-shadow:0 4px 16px rgba(59,130,246,.35);border:1px solid rgba(255,255,255,.15)';
    banner.innerHTML = '<span style="display:flex;align-items:center;gap:10px"><span style="font-size:1.6rem">🧭</span><span>Head north on Pier J — 0.3 mi to first turn</span></span><span id="NavDist_' + jobId + '" style="font-size:.95rem;font-weight:700;opacity:.95;background:rgba(0,0,0,.2);padding:6px 12px;border-radius:6px">Step 1/12 · 22.4 km left</span>';
    mapEl.parentNode.insertBefore(banner, mapEl);
  }
  // For demo: simulate driver progress along the Valhalla coords. Real GPS path takes over
  // automatically when simulatedPath is omitted on a driver's actual device.
  const simulate = !!opts_simulate();
  let nav = null;
  function opts_simulate() { return localStorage.getItem('wolfs_nav_simulate') !== 'false'; }
  function buildOpts(routeCoords) {
    return {
      map: m,
      origin, destination, stops,
      simulatedPath: simulate && routeCoords ? routeCoords : null,
      simulatedTickMs: 600,
      onTurn: ({ instruction, distanceM, stepIndex, totalSteps }) => {
        const distLabel = distanceM > 1000 ? (distanceM/1000).toFixed(1)+' km' : Math.round(distanceM)+' m';
        banner.innerHTML = '<span style="display:flex;align-items:center;gap:10px"><span style="font-size:1.6rem">🧭</span><span>' + (instruction || 'Continue').replace(/[<>]/g, '') + ' · in ' + distLabel + '</span></span><span id="NavDist_' + jobId + '" style="font-size:.95rem;font-weight:700;opacity:.95;background:rgba(0,0,0,.2);padding:6px 12px;border-radius:6px">Step ' + (stepIndex + 1) + '/' + totalSteps + '</span>';
      },
      onProgress: ({ distToDestM }) => {
        const distEl = document.getElementById('NavDist_' + jobId);
        if (distEl) distEl.textContent = (distToDestM / 1000).toFixed(2) + ' km to destination';
      },
      onArrive: () => {
        banner.style.background = '#22c55e';
        banner.innerHTML = '<span>✅ Arrived at destination — tap "Complete delivery" below.</span>';
      },
      onOffRoute: () => {
        if (!window.__navStarted || (Date.now() - window.__navStarted) < 6000) return;
        banner.style.background = '#f59e0b';
        banner.innerHTML = '<span>⚠ Off route — recalculating…</span>';
        setTimeout(() => { banner.style.background = 'linear-gradient(135deg,#3b82f6,#2563eb)'; }, 2500);
      },
    };
  }
  window.__navStarted = Date.now();
  nav = WolfsNav.start({ ...buildOpts(null), onProgress: () => {} });
  // Mirror the route polyline onto the main dashboard map (MapInstance) so the driver sees
  // the route on the BIG map, not just the small inline JobRouteMap. Polls every 500ms for
  // up to 30s waiting for Valhalla/OSRM to return coords.
  if (window.MapInstance) {
    if (window.__mainRouteLayer) { try { window.MapInstance.removeLayer(window.__mainRouteLayer); } catch (_) {} window.__mainRouteLayer = null; }
    if (window.__mainRoutePin) { try { window.MapInstance.removeLayer(window.__mainRoutePin); } catch (_) {} window.__mainRoutePin = null; }
    if (window.__mainRouteEnd) { try { window.MapInstance.removeLayer(window.__mainRouteEnd); } catch (_) {} window.__mainRouteEnd = null; }
    let attempts = 0;
    const mirrorInterval = setInterval(() => {
      attempts++;
      const r = nav && nav.route && nav.route();
      if (r && r.coords && r.coords.length > 1) {
        clearInterval(mirrorInterval);
        try {
          window.__mainRouteLayer = L.polyline(r.coords, { color: '#3b82f6', weight: 6, opacity: .9 }).addTo(window.MapInstance);
          // Pickup pin (green) + delivery pin (orange) on the main map
          window.__mainRoutePin = L.circleMarker(r.coords[0], { radius: 9, color: '#22c55e', fillColor: '#22c55e', fillOpacity: 1, weight: 3 }).addTo(window.MapInstance).bindTooltip('Pickup', {permanent:false});
          window.__mainRouteEnd = L.circleMarker(r.coords[r.coords.length - 1], { radius: 9, color: '#ff6b35', fillColor: '#ff6b35', fillOpacity: 1, weight: 3 }).addTo(window.MapInstance).bindTooltip('Delivery', {permanent:false});
          window.MapInstance.fitBounds(window.__mainRouteLayer.getBounds(), { padding: [60, 60] });
        } catch (_) {}
      } else if (attempts > 60) {
        clearInterval(mirrorInterval);
      }
    }, 500);
  }
  // After Valhalla returns, swap into a simulated run along its real geometry.
  setTimeout(() => {
    const r = nav && nav.route && nav.route();
    if (simulate && r && r.coords && r.coords.length > 4) {
      nav.stop();
      nav = WolfsNav.start(buildOpts(r.coords));
    }
  }, 4000);
  window.__activeNav = nav;
}
window.StartTurnByTurn = StartTurnByTurn;

