// ─── Realtime driver tracking simulation ─────────────────────────────
// Once a job is accepted, simulate the driver moving along the OSRM route.
// We sample the route polyline at even intervals and advance a green driver
// marker every ~220ms. A status banner reports "ON ROUTE" (green) as long as
// the driver stays within 120 m of the polyline; if we deliberately push them
// off the polyline to demo the detection, the banner flips to "OFF ROUTE"
// (orange) and the platform recalculates from the new position to the
// remaining waypoints.
window.__driverTracking = {};
function _distMeters(a, b) {
  // Haversine
  const R = 6371000;
  const lat1 = a[0] * Math.PI / 180, lat2 = b[0] * Math.PI / 180;
  const dLat = (b[0] - a[0]) * Math.PI / 180;
  const dLon = (b[1] - a[1]) * Math.PI / 180;
  const h = Math.sin(dLat/2)**2 + Math.cos(lat1)*Math.cos(lat2)*Math.sin(dLon/2)**2;
  return 2 * R * Math.asin(Math.sqrt(h));
}
function _closestPointOnPolyline(pt, poly) {
  let best = Infinity;
  for (const p of poly) { const d = _distMeters([pt.lat, pt.lng], [p.lat, p.lng]); if (d < best) best = d; }
  return best;
}
async function StartDriverTracking(jobId, opts) {
  opts = opts || {};
  const mapEl = document.getElementById('JobRouteMap_' + jobId);
  if (!mapEl) return;
  mapEl.style.display = 'block';
  const infoEl = document.getElementById('JobRouteInfo_' + jobId);
  if (infoEl) infoEl.style.display = 'block';
  if (!window.__driverRouteMaps[jobId]) await PreviewJobRoute(jobId);
  const m = window.__driverRouteMaps[jobId];
  if (!m) return;
  m.invalidateSize();
  // Wait for the route polyline to exist (routesfound adds it to m._rc).
  for (let t = 0; t < 40 && (!m._rc || !m._rc._routes || !m._rc._routes[0]); t++) {
    await new Promise(r => setTimeout(r, 200));
  }
  const route = m._rc && m._rc._routes && m._rc._routes[0];
  if (!route || !route.coordinates || route.coordinates.length < 2) { if (infoEl) infoEl.textContent = 'No route to track.'; return; }
  const poly = route.coordinates;

  // Banner on top of the map showing On route / Off route.
  let banner = mapEl.querySelector('.TrackBanner');
  if (!banner) {
    banner = document.createElement('div');
    banner.className = 'TrackBanner';
    banner.style.cssText = 'position:absolute;top:8px;right:8px;padding:6px 12px;border-radius:8px;font-weight:800;font-size:.78rem;letter-spacing:.3px;text-transform:uppercase;z-index:500;box-shadow:0 3px 10px rgba(0,0,0,.4)';
    mapEl.style.position = 'relative';
    mapEl.appendChild(banner);
  }
  function setStatus(ok, text) {
    banner.style.background = ok ? '#22c55e' : '#f59e0b';
    banner.style.color = '#0f1419';
    banner.textContent = text;
  }
  // Moving driver pin.
  const state = { idx: 0, marker: null, canceled: false };
  window.__driverTracking[jobId] = state;
  state.marker = L.marker(poly[0], { icon: L.divIcon({ className:'', html:'<div style="width:30px;height:30px;border-radius:50%;background:#22c55e;border:4px solid #0f1419;box-shadow:0 4px 12px rgba(0,0,0,.6);display:flex;align-items:center;justify-content:center;color:#fff;font-size:.85rem;font-weight:900">🚚</div>', iconSize:[30,30], iconAnchor:[15,15] }) }).addTo(m);
  setStatus(true, 'ON ROUTE · 0%');

  const total = poly.length;
  const step = Math.max(1, Math.floor(total / (opts.frames || 20)));
  for (let i = 0; i < total && !state.canceled; i += step) {
    const p = poly[i];
    state.marker.setLatLng(p);
    m.panTo(p);
    const pct = Math.round((i / (total - 1)) * 100);
    // Simulate going off route at ~60% if requested.
    if (opts.deviateAt && pct >= opts.deviateAt && !state.deviated) {
      state.deviated = true;
      // Shift the pin 300m south-east of the route (off-route).
      const off = L.latLng(p.lat - 0.003, p.lng + 0.003);
      state.marker.setLatLng(off);
      const dist = _closestPointOnPolyline(off, poly);
      setStatus(false, 'OFF ROUTE · ' + Math.round(dist) + ' m');
      if (infoEl) infoEl.innerHTML += ' · <span style="color:#f59e0b;font-weight:700">⚠️ off-course — recalculating…</span>';
      await new Promise(r => setTimeout(r, 1400));
      // "Recalculate" — animate back onto the polyline at the same index.
      setStatus(true, 'ON ROUTE · ' + pct + '% (recalculated)');
      state.marker.setLatLng(p);
      if (infoEl) infoEl.innerHTML = infoEl.innerHTML.replace('⚠️ off-course — recalculating…', '<span style="color:#22c55e;font-weight:700">✓ back on route</span>');
    } else {
      setStatus(true, 'ON ROUTE · ' + pct + '%');
    }
    await new Promise(r => setTimeout(r, opts.tickMs || 220));
  }
  if (!state.canceled) setStatus(true, 'DELIVERED · 100%');
}
function StopDriverTracking(jobId) {
  const state = window.__driverTracking[jobId];
  if (state) state.canceled = true;
}
window.StartDriverTracking = StartDriverTracking;
window.StopDriverTracking = StopDriverTracking;
async function AcceptRealJob(jobId) {
  var email = (localStorage.getItem('wolfs_email') || '').toLowerCase();
  var [workers, jobs] = await Promise.all([WolfsDB.all('workers'), WolfsDB.all('jobs')]);
  var me = workers.find(function(w){return (w.email||'').toLowerCase()===email});
  if (!me) return;
  var j = jobs.find(function(x){return x.id===jobId});
  if (!j) return;
  j.status = 'accepted'; j.acceptedBy = me.id; j.acceptedAt = new Date().toISOString();
  await WolfsDB.put('jobs', j);
  await WolfsDB.put('timesheets', { id:'ts_'+Date.now(), jobId:j.id, workerId:me.id, startsAt:new Date().toISOString(), hours:j.hours, payRate:j.payRate, status:'in_progress' });
  // Start the live-location writer so employer + dispatcher can see the truck. Geolocation
  // permission is requested once; if denied (or unavailable), the writer just logs to console.
  try {
    if (window.__activeTracker) window.__activeTracker.stop();
    if (window.WolfsTracker) {
      window.__activeTracker = WolfsTracker.start({ driverId: me.id, jobId: j.id, intervalMs: 3000, onError: function(e){console.warn('[tracker]', e && e.message || e);} });
    }
  } catch (ex) { console.warn('[tracker] start failed', ex); }
  alert('Accepted! Timesheet opened. Sharing your live location with the employer until you complete the job.');
  RenderRealJobs();
}

// Hiring Hall integration — finds the logged-in driver's assignments in the shared browser DB.
async function RenderHallForDriver() {
  try {
    await WolfsDB.seed();
    var email = (localStorage.getItem('wolfs_email') || '').toLowerCase();
    var name = localStorage.getItem('wolfs_name') || '';
    var [workers, schedules, roles, customers] = await Promise.all([WolfsDB.all('workers'), WolfsDB.all('schedules'), WolfsDB.all('roles'), WolfsDB.all('customers')]);
    // Best-effort worker identification by email, then name, then first seeded worker.
    var me = workers.find(function(w){ return (w.email||'').toLowerCase() === email; })
          || workers.find(function(w){ return w.name === name; })
          || workers[0];
    if (!me) { document.getElementById('HallContent').innerHTML = '<p style="color:var(--text-muted)">No worker record found.</p>'; return; }
    var rmap = Object.fromEntries(roles.map(function(r){return [r.id, r.name]}));
    var cmap = Object.fromEntries(customers.map(function(c){return [c.id, c.name]}));
    var mine = [];
    for (var s of schedules) {
      var a = (s.assignments||[]).find(function(x){return x.workerId === me.id});
      if (a) mine.push({ s: s, a: a });
    }
    mine.sort(function(x,y){return x.s.startsAt.localeCompare(y.s.startsAt)});
    document.getElementById('HallBadge').textContent = mine.length + ' assignment' + (mine.length === 1 ? '' : 's');
    if (!mine.length) {
      document.getElementById('HallContent').innerHTML = '<p style="color:var(--text-muted);padding:12px 0">No hiring hall schedules yet. Admins can create them at <a href="/wolfstruckingco.com/HiringHall/" style="color:var(--accent)">/HiringHall/</a> → Scheduling.</p>';
      return;
    }
    document.getElementById('HallContent').innerHTML = mine.map(function(e){
      var color = e.a.status === 'confirmed' ? 'var(--success,#22c55e)' : e.a.status === 'rejected' ? 'var(--danger,#ef4444)' : 'var(--warning,#f59e0b)';
      var actions = e.a.status === 'pending'
        ? '<button onclick="DriverRespond(\'' + e.s.id + '\',\'confirmed\')" style="background:#22c55e;color:#fff;border:none;padding:6px 12px;border-radius:6px;margin-right:6px;cursor:pointer;font-weight:700">Confirm</button>' +
          '<button onclick="DriverRespond(\'' + e.s.id + '\',\'rejected\')" style="background:#ef4444;color:#fff;border:none;padding:6px 12px;border-radius:6px;cursor:pointer;font-weight:700">Reject</button>'
        : '';
      return '<div style="padding:12px;border:1px solid var(--border,#2a3a4a);border-radius:8px;margin-bottom:10px">' +
        '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px"><b>' + (rmap[e.s.roleId]||e.s.roleId) + '</b><span style="color:' + color + ';text-transform:uppercase;font-size:.72rem;letter-spacing:.4px">' + e.a.status + '</span></div>' +
        '<div style="color:var(--text-muted);font-size:.82rem;margin-bottom:4px">' + (cmap[e.s.customerId]||'') + '</div>' +
        '<div style="font-size:.85rem;margin-bottom:8px">' + e.s.startsAt + ' → ' + e.s.endsAt + '</div>' +
        (e.s.instructions ? '<div style="color:var(--text-muted);font-size:.78rem;margin-bottom:8px">📋 ' + e.s.instructions + '</div>' : '') +
        actions + '</div>';
    }).join('');
  } catch (ex) {
    document.getElementById('HallContent').innerHTML = '<p style="color:var(--danger,#ef4444)">Error loading schedules: ' + (ex.message || ex) + '</p>';
  }
}
async function DriverRespond(scheduleId, status) {
  var s = await WolfsDB.get('schedules', scheduleId);
  if (!s) return;
  var email = (localStorage.getItem('wolfs_email') || '').toLowerCase();
  var name = localStorage.getItem('wolfs_name') || '';
  var workers = await WolfsDB.all('workers');
  var me = workers.find(function(w){ return (w.email||'').toLowerCase() === email; })
        || workers.find(function(w){ return w.name === name; }) || workers[0];
  var a = (s.assignments||[]).find(function(x){return x.workerId === me.id});
  if (a) { a.status = status; a.respondedAt = new Date().toISOString(); }
  // Auto-reassign on reject
  if (status === 'rejected' && s.autoReassign) {
    var confirmedCount = (s.assignments||[]).filter(function(x){return x.status==='confirmed'}).length;
    if (confirmedCount < (s.target||1)) {
      var roles = await WolfsDB.all('roles');
      var role = roles.find(function(r){return r.id===s.roleId}) || { requiredBadges: [] };
      var offered = new Set((s.assignments||[]).map(function(x){return x.workerId}));
      var next = workers.find(function(w){ return w.approved && !offered.has(w.id) && (w.roles||[]).includes(s.roleId) && (role.requiredBadges||[]).every(function(bid){return (w.badges||[]).includes(bid)}); });
      if (next) s.assignments.push({ workerId: next.id, status:'pending', offeredAt: new Date().toISOString(), reassignedFrom: me.id });
    }
  }
  // Generate timesheet on confirm
  if (status === 'confirmed') {
    var hours = (new Date(s.endsAt) - new Date(s.startsAt)) / 36e5;
    await WolfsDB.put('timesheets', { id:'ts_'+WolfsDB.uuid(), scheduleId:s.id, workerId:me.id, startsAt:s.startsAt, endsAt:s.endsAt, hours, status: new Date(s.endsAt) < new Date() ? 'completed' : (new Date(s.startsAt) > new Date() ? 'future' : 'waiting'), createdAt: new Date().toISOString() });
  }
  await WolfsDB.put('schedules', s);
  RenderHallForDriver();
}
