function ClassifySegment(Lat, Lng) {
  for (var I = 0; I < UrbanZones.length; I++) {
    var Z = UrbanZones[I];
    var D = Math.sqrt(Math.pow(Lat - Z.lat, 2) + Math.pow(Lng - Z.lng, 2));
    if (D < Z.radius) return 'red';
  }
  if (Lat > 34.5 || Lat < 33.5) return 'green';
  return 'yellow';
}

function DrawRoute() {
  if (RoutingControl) { MapInstance.removeControl(RoutingControl); RoutingControl = null; }
  if (TrafficLayer) { MapInstance.removeLayer(TrafficLayer); TrafficLayer = null; }

  var Start = L.latLng(34.1808, -118.3090);
  var End = L.latLng(33.7405, -118.2716);

  RoutingControl = L.Routing.control({
    waypoints: [Start, End],
    router: L.Routing.osrmv1({ serviceUrl: 'https://router.project-osrm.org/route/v1' }),
    lineOptions: { styles: [{ color: '#22c55e', weight: 5, opacity: 0.7 }] },
    show: false,
    addWaypoints: false,
    draggableWaypoints: false,
    fitSelectedRoutes: true,
    createMarker: function() { return null; }
  }).addTo(MapInstance);

  RoutingControl.on('routesfound', function(E) {
    var Coords = E.routes[0].coordinates;
    TrafficLayer = L.layerGroup().addTo(MapInstance);
    for (var I = 0; I < Coords.length - 1; I++) {
      var A = Coords[I];
      var B = Coords[I + 1];
      var MidLat = (A.lat + B.lat) / 2;
      var MidLng = (A.lng + B.lng) / 2;
      var Traffic = ClassifySegment(MidLat, MidLng);
      var Color = Traffic === 'red' ? '#ef4444' : Traffic === 'yellow' ? '#eab308' : '#22c55e';
      L.polyline([[A.lat, A.lng], [B.lat, B.lng]], {
        color: Color, weight: 7, opacity: 0.85
      }).addTo(TrafficLayer);
    }
  });
}

function ShowView(View) {
  document.getElementById('JobsView').style.display = View === 'jobs' ? 'block' : 'none';
  document.getElementById('KpisView').style.display = View === 'kpis' ? 'block' : 'none';
  var HallView = document.getElementById('HallView');
  if (HallView) HallView.style.display = View === 'hall' ? 'block' : 'none';
  document.getElementById('ActiveView').className = 'ActiveDelivery' + (View === 'active' ? ' Show' : '');
  document.querySelectorAll('.MenuItem').forEach(function(M){M.classList.remove('Active')});

  if (View === 'hall' && typeof RenderHallForDriver === 'function') RenderHallForDriver();
  if (View === 'jobs' && typeof RenderRealJobs === 'function') RenderRealJobs();

  if (View === 'jobs' || View === 'kpis' || View === 'hall') {
    document.getElementById('LeftPanel').classList.remove('Collapsed');
    var tl = document.getElementById('ToggleLeft'); if (tl) tl.classList.remove('Collapsed');
    var ro = document.getElementById('ReopenLeft'); if (ro) ro.classList.remove('Show');
  }
  if (View === 'active') {
    document.getElementById('RightPanel').classList.remove('Collapsed');
    document.getElementById('ToggleRight').classList.remove('Collapsed');
  }
  if (View === 'kpis') RenderKpis();
}

function ToggleLeftPanel() {
  var lp = document.getElementById('LeftPanel');
  var tl = document.getElementById('ToggleLeft');
  var ro = document.getElementById('ReopenLeft');
  lp.classList.toggle('Collapsed');
  if (tl) tl.classList.toggle('Collapsed');
  if (ro) ro.classList.toggle('Show', lp.classList.contains('Collapsed'));
  setTimeout(function() {
    try { MapInstance.invalidateSize(); } catch (_) {}
    if (typeof window.__centerPinInVisible === 'function') window.__centerPinInVisible();
  }, 350);
}

function ToggleRightPanel() {
  document.getElementById('RightPanel').classList.toggle('Collapsed');
  var tr = document.getElementById('ToggleRight'); if (tr) tr.classList.toggle('Collapsed');
  setTimeout(function() {
    try { MapInstance.invalidateSize(); } catch (_) {}
    if (typeof window.__centerPinInVisible === 'function') window.__centerPinInVisible();
  }, 350);
}

async function RenderKpis() {
  const box = document.getElementById('KpiContent');
  if (!box) return;
  try {
    const email = (localStorage.getItem('wolfs_email') || '').toLowerCase();
    const [workers, jobs, timesheets] = await Promise.all([
      WolfsDB.all('workers'),
      WolfsDB.all('jobs'),
      WolfsDB.all('timesheets'),
    ]);
    const me = workers.find(w => (w.email || '').toLowerCase() === email) || workers.find(w => w.id === 'wkr_driver');
    const myId = me && me.id;
    const myTs = timesheets.filter(t => t.workerId === myId);
    const completed = myTs.filter(t => t.status === 'completed');
    const nowMs = Date.now();
    const weekCutoff = nowMs - 7 * 24 * 60 * 60 * 1000;
    const weekEarnings = completed
      .filter(t => new Date(t.endsAt || t.startsAt || 0).getTime() >= weekCutoff)
      .reduce((a, t) => a + (t.earnings || 0), 0);
    const lifetime = (me && me.totalEarnings) || completed.reduce((a, t) => a + (t.earnings || 0), 0);
    const onTime = completed.length
      ? Math.round((completed.filter(t => t.onTime !== false).length / completed.length) * 100)
      : null;
    const rating = (me && me.rating) || null;

    const tile = (value, label, color) =>
      `<div style="background:var(--bg);border:1px solid var(--border);border-radius:12px;padding:20px;text-align:center">`
      + `<div style="font-size:1.8rem;font-weight:900;color:${color}">${value}</div>`
      + `<div style="font-size:.8rem;color:var(--text-muted);margin-top:4px">${label}</div></div>`;

    const kpiTiles = [
      tile(onTime == null ? '—' : onTime + '%', 'On-Time Rate', 'var(--success)'),
      tile('$' + Math.round(weekEarnings).toLocaleString(), 'This Week', 'var(--accent)'),
      tile(String(completed.length), 'Completed Loads', 'var(--text)'),
      tile(rating ? rating.toFixed(1) + '★' : '—', 'Rating', 'var(--warning)'),
    ].join('');

    const jobById = Object.fromEntries(jobs.map(j => [j.id, j]));
    const recent = completed
      .slice()
      .sort((a, b) => new Date(b.endsAt || b.startsAt || 0) - new Date(a.endsAt || a.startsAt || 0))
      .slice(0, 5);
    const recentRows = recent.length
      ? recent.map(t => {
          const j = jobById[t.jobId] || {};
          const when = new Date(t.endsAt || t.startsAt || Date.now());
          const dateStr = when.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
          const pay = (t.earnings || (t.hours || 0) * (t.payRate || 0)).toFixed(0);
          return `<div class="JobCard"><div class="JobCardHeader"><div class="JobTitle">${(j.title || 'Delivery').replace(/</g,'&lt;')}</div><div style="color:var(--success);font-weight:700">$${pay}</div></div><div style="font-size:.8rem;color:var(--text-muted)">${dateStr} · ${(j.pickup||'').replace(/</g,'&lt;')} → ${(j.delivery||'').replace(/</g,'&lt;')}</div></div>`;
        }).join('')
      : '<p style="color:var(--text-muted);padding:10px;font-size:.85rem">No completed deliveries yet. Accept a job to start earning.</p>';

    box.innerHTML = ''
      + '<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:20px">' + kpiTiles + '</div>'
      + `<div style="display:flex;justify-content:space-between;align-items:baseline;margin-bottom:12px"><h4 style="font-size:.9rem;color:var(--text-muted);margin:0">RECENT DELIVERIES</h4><span style="color:var(--text-muted);font-size:.78rem">Lifetime: $${Math.round(lifetime).toLocaleString()}</span></div>`
      + recentRows;
  } catch (ex) {
    console.error('RenderKpis failed:', ex);
    box.innerHTML = '<p style="color:var(--text-muted);padding:10px">Could not load your earnings right now.</p>';
  }
}

function ToggleMenu() {
  document.getElementById('SideMenu').classList.toggle('Open');
  document.getElementById('MenuOverlay').classList.toggle('Open');
}

function OpenChat() {
  document.getElementById('ChatPanel').classList.add('Open');
}

function CloseChat() {
  document.getElementById('ChatPanel').classList.remove('Open');
}

function SendToRelay(Data) {
  fetch(RelayUrl + '/send', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify(Data)
  }).catch(function() {});
}

var DemoChatReplies = [
  'Roger that. Traffic is clear on I-85 South. ETA to pickup is 22 minutes.',
  'Copy. Load confirmed at pickup. Seal number 4821907 recorded in system.',
  'Route updated — detour via US-321 to avoid I-40 construction. Saves 8 minutes.',
  'Weather alert: light rain expected near Charlotte. Drive safe, no hazard.',
  'Great work! Delivery on-time. Signature captured. Payment processing.',
  'Next load available: Container Drayage, $285, ready in 45 minutes. Want it?',
  'Dispatch here. What do you need?'
];
var DemoChatIdx = 0;

function SendChat() {
  var Input = document.getElementById('ChatInputField');
  var Text = Input.value.trim();
  if (!Text) return;
  Input.value = '';
  AddChatMsg(Text, true);
  if (localStorage.getItem('wolfs_demo') === 'true') {
    setTimeout(function() {
      AddChatMsg(DemoChatReplies[DemoChatIdx % DemoChatReplies.length], false);
      DemoChatIdx++;
    }, 1000 + Math.random() * 500);
  } else {
    SendToRelay({type:'chat',content:Text,id:'chat_'+Date.now()});
  }
}

function AddChatMsg(Text, IsUser) {
  var Container = document.getElementById('ChatMessages');
  var Msg = document.createElement('div');
  Msg.className = 'ChatMsg ' + (IsUser ? 'ChatMsgUser' : 'ChatMsgBot');
  Msg.textContent = Text;
  Container.appendChild(Msg);
  Container.scrollTop = Container.scrollHeight;
}

