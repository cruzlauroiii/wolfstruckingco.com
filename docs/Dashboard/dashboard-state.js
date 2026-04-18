var RelayUrl = 'https://wolfstruckingco.nbth.workers.dev';
var MapInstance = null;
var DriverMarker = null;
var RouteLayer = null;
var ActiveJob = null;
var ChecklistDone = 0;
var ChecklistTotal = 8;
var ChatVoiceActive = false;
var ChatSpeechRec = null;

(function AuthCheck() {
  if (!localStorage.getItem('wolfs_session')) {
    window.location.href = '/wolfstruckingco.com/Login/';
    return;
  }
  var Email = localStorage.getItem('wolfs_email') || 'driver@wolfstruckingco.com';
  var Name = Email.split('@')[0].replace(/[._]/g,' ').replace(/\b\w/g, function(L){return L.toUpperCase()});
  var DriverNameEl = document.getElementById('DriverName');
  if (DriverNameEl) DriverNameEl.textContent = Name;
  var MenuUserNameEl = document.getElementById('MenuUserName');
  if (MenuUserNameEl) MenuUserNameEl.textContent = Name;
  var MenuUserEmailEl = document.getElementById('MenuUserEmail');
  if (MenuUserEmailEl) MenuUserEmailEl.textContent = Email;
  window.RefreshTopKpis = async function() {
    try {
      var e = (localStorage.getItem('wolfs_email') || '').toLowerCase();
      var [workers, timesheets] = await Promise.all([WolfsDB.all('workers'), WolfsDB.all('timesheets')]);
      var me = workers.find(function(w){return (w.email||'').toLowerCase()===e;}) || workers.find(function(w){return w.id==='wkr_driver';});
      var myTs = timesheets.filter(function(t){return me && t.workerId===me.id;});
      var completed = myTs.filter(function(t){return t.status==='completed';});
      var todayStart = new Date(); todayStart.setHours(0,0,0,0);
      var todayEarnings = completed
        .filter(function(t){return new Date(t.endsAt||t.startsAt||0).getTime() >= todayStart.getTime();})
        .reduce(function(a,t){return a+(t.earnings||0);}, 0);
      var onTime = completed.length
        ? Math.round((completed.filter(function(t){return t.onTime!==false;}).length / completed.length) * 100)
        : null;
      var onEl = document.getElementById('KpiOnTime');
      var earnEl = document.getElementById('KpiEarnings');
      if (onEl) onEl.textContent = (onTime==null ? '—' : onTime + '%') + ' On-Time';
      if (earnEl) earnEl.textContent = '$' + Math.round(todayEarnings).toLocaleString() + ' Today';
    } catch (ex) { console.error('RefreshTopKpis failed:', ex); }
  };
  if (window.WolfsDB) RefreshTopKpis();
  else setTimeout(function waitDb(){ if (window.WolfsDB) RefreshTopKpis(); else setTimeout(waitDb, 120); }, 120);
})();

var Jobs = [
  {Id:1,Title:'Container Drayage - Port of LA',Pickup:'Port of Los Angeles, Terminal Island',Delivery:'3PL Warehouse, Commerce CA',Pay:285,Distance:'22 mi',Time:'2-3 hrs',Weight:'42,000 lbs',Type:'Container',Window:'Today 8:00 AM - 2:00 PM',Notes:'TWIC required. 40ft container. Chassis provided at terminal.'},
  {Id:2,Title:'Regional LTL - San Fernando Valley',Pickup:'Burbank Distribution Center',Delivery:'Multiple stops - Northridge, Chatsworth, Simi Valley',Pay:340,Distance:'68 mi',Time:'4-5 hrs',Weight:'18,500 lbs',Type:'LTL',Window:'Today 6:00 AM - 4:00 PM',Notes:'6 stops total. Liftgate needed for 2 stops. Residential deliveries.'},
  {Id:3,Title:'Dedicated Run - Walmart DC',Pickup:'Wolfs Warehouse, Burbank',Delivery:'Walmart DC, Fontana CA',Pay:195,Distance:'52 mi',Time:'2 hrs',Weight:'38,000 lbs',Type:'FTL',Window:'Today 10:00 AM - 1:00 PM',Notes:'Drop and hook. Appointment at 11:30 AM. No lumper needed.'},
  {Id:4,Title:'Reefer Load - Produce',Pickup:'LA Wholesale Produce Market',Delivery:'Ralphs Distribution, Compton',Pay:310,Distance:'18 mi',Time:'3 hrs',Weight:'35,200 lbs',Type:'Reefer',Window:'Today 5:00 AM - 10:00 AM',Notes:'Temperature 34F. Pre-cool required. FIFO loading.'},
  {Id:5,Title:'Flatbed - Construction Materials',Pickup:'Home Depot Supply, Irwindale',Delivery:'Construction Site, Downtown LA',Pay:275,Distance:'28 mi',Time:'3-4 hrs',Weight:'44,000 lbs',Type:'Flatbed',Window:'Tomorrow 7:00 AM - 12:00 PM',Notes:'Tarping required. Oversized load permit on file. Escort not needed.'},
  {Id:6,Title:'Express Delivery - Medical Supply',Pickup:'McKesson DC, Santa Fe Springs',Delivery:'Cedars-Sinai Medical Center, LA',Pay:165,Distance:'15 mi',Time:'1.5 hrs',Weight:'8,200 lbs',Type:'Express',Window:'Today ASAP',Notes:'Priority delivery. White glove service. Loading dock access.'}
];

var ChecklistItems = [
  'Engine oil level checked',
  'Tire pressure and condition inspected',
  'Brake system tested',
  'Lights and signals functioning',
  'Mirrors clean and adjusted',
  'Coupling devices secure',
  'Cargo properly loaded and secured',
  'All paperwork and permits ready'
];

function InitMap() {
  // Driver pin always at this fixed lat/lng — exposed so panel-toggle handlers can re-center.
  window.__driverHomeLatLng = [34.1808, -118.3090];
  MapInstance = L.map('Map',{zoomControl:false,maxZoom:19}).setView(window.__driverHomeLatLng, 19);
  // Zoom control bottom-left so it doesn't overlap the burger menu (top-left).
  L.control.zoom({position:'bottomleft'}).addTo(MapInstance);
  // Re-center so the pin sits in the visible map area (right of LeftPanel / left of RightPanel),
  // not behind the panels. Account for current panel widths every call so it stays aligned
  // when panels toggle.
  window.__centerPinInVisible = function() {
    if (!MapInstance) return;
    var lp = document.getElementById('LeftPanel');
    var rp = document.getElementById('RightPanel');
    var lw = lp && !lp.classList.contains('Collapsed') ? lp.offsetWidth : 0;
    var rw = rp && !rp.classList.contains('Collapsed') ? rp.offsetWidth : 0;
    var w = MapInstance.getSize().x;
    var visibleCenterX = lw + (w - lw - rw) / 2;
    var actualCenterX = w / 2;
    var dx = actualCenterX - visibleCenterX;
    // Reset to the home point first, then offset.
    MapInstance.setView(window.__driverHomeLatLng, MapInstance.getZoom(), { animate: false });
    if (dx !== 0) MapInstance.panBy([dx, 0], { animate: false });
  };
  MapInstance.whenReady(function() {
    requestAnimationFrame(function() { window.__centerPinInVisible(); });
    setTimeout(function() { window.__centerPinInVisible(); }, 300);
  });
  L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',{
    maxZoom:19,
    subdomains:'abcd'
  }).addTo(MapInstance);
  var DriverIcon = L.divIcon({
    html:'<div style="width:36px;height:36px;background:#ff6b35;border-radius:50%;border:3px solid #fff;display:flex;align-items:center;justify-content:center;font-size:18px;box-shadow:0 2px 8px rgba(0,0,0,.4)">&#128666;</div>',
    iconSize:[36,36],
    iconAnchor:[18,18],
    className:''
  });
  DriverMarker = L.marker([34.1808,-118.3090],{icon:DriverIcon}).addTo(MapInstance);
  DriverMarker.bindPopup('<b>Your Location</b><br>Burbank, CA');
  var Locations = [
    [33.7405,-118.2716,'Port of LA'],
    [34.0006,-118.1590,'Commerce 3PL'],
    [34.1808,-118.3090,'Wolfs HQ'],
    [34.0252,-117.5674,'Fontana DC'],
    [34.0337,-118.2631,'Downtown LA']
  ];
  Locations.forEach(function(Loc) {
    var Icon = L.divIcon({
      html:'<div style="width:12px;height:12px;background:#22c55e;border-radius:50%;border:2px solid #0f1419;box-shadow:0 0 6px rgba(34,197,94,.5)"></div>',
      iconSize:[12,12],
      iconAnchor:[6,6],
      className:''
    });
    L.marker([Loc[0],Loc[1]],{icon:Icon}).addTo(MapInstance).bindPopup(Loc[2]);
  });
}

function RenderJobs() {
  // No mock jobs: the only Available Jobs the driver ever sees are real employer-posted
  // jobs that match their badges, rendered by RenderRealJobs() into #RealJobList.
  document.getElementById('JobList').innerHTML = '';
  document.getElementById('JobCount').textContent = '0 Available';
}

function ToggleJobDetails(Id) {
  var El = document.getElementById('JobExp_' + Id);
  var WasOpen = El.classList.contains('Show');
  document.querySelectorAll('.JobExpanded').forEach(function(E){E.classList.remove('Show')});
  if (!WasOpen) El.classList.add('Show');
}

function AcceptJob(Id) {
  ActiveJob = Jobs.find(function(J){return J.Id === Id});
  if (!ActiveJob) return;
  ShowView('active');
  document.getElementById('ActiveTitle').textContent = ActiveJob.Title;
  document.getElementById('RightPanel').classList.remove('Collapsed');
  document.getElementById('ToggleRight').classList.remove('Collapsed');
  RenderChecklist();
  DrawRoute();
  // Persist acceptance to R2 (real business mode)
  if (ActiveJob.RealId && localStorage.getItem('wolfs_demo') !== 'true') {
    var DriverEmail = localStorage.getItem('wolfs_email') || '';
    fetch(RelayUrl + '/api/jobs/' + ActiveJob.RealId, { method:'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ status:'accepted', acceptedBy: DriverEmail, acceptedAt: new Date().toISOString() }) }).catch(function(){});
    fetch(RelayUrl + '/api/deliveries', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ id:'del_'+Date.now(), jobId: ActiveJob.RealId, driverEmail: DriverEmail, status:'in_progress', startedAt: new Date().toISOString() }) }).catch(function(){});
  }
}

function RenderChecklist() {
  ChecklistDone = 0;
  var Html = '';
  ChecklistItems.forEach(function(Item, I) {
    Html += '<div class="ChecklistItem">' +
      '<div class="ChecklistBox" id="Check_' + I + '" onclick="ToggleCheck(' + I + ')"></div>' +
      '<div class="ChecklistLabel">' + Item + '</div>' +
    '</div>';
  });
  document.getElementById('ChecklistItems').innerHTML = Html;
  document.getElementById('BtnStartDrive').disabled = true;
}

function ToggleCheck(I) {
  var El = document.getElementById('Check_' + I);
  if (El.classList.contains('Checked')) {
    El.classList.remove('Checked');
    El.innerHTML = '';
    ChecklistDone--;
  } else {
    El.classList.add('Checked');
    El.innerHTML = '&#10003;';
    ChecklistDone++;
  }
  document.getElementById('BtnStartDrive').disabled = (ChecklistDone < ChecklistTotal);
}

function StartDriving() {
  document.getElementById('PreTrip').style.display = 'none';
  document.getElementById('DrivingView').style.display = 'block';
  var Steps = [
    'Depart from current location',
    'Head to pickup: ' + (ActiveJob ? ActiveJob.Pickup : ''),
    'Arrive at pickup, load cargo',
    'Verify load count and seal number',
    'Depart pickup, head to delivery',
    'Follow designated route (see map)',
    'Arrive at delivery: ' + (ActiveJob ? ActiveJob.Delivery : ''),
    'Unload cargo, get signature',
    'Submit delivery confirmation'
  ];
  var Html = '';
  Steps.forEach(function(S, I) {
    Html += '<div class="DeliveryStep"><div class="StepNum" id="Step_' + I + '">' + (I+1) + '</div><div class="StepText">' + S + '</div></div>';
  });
  document.getElementById('DeliverySteps').innerHTML = Html;
}

function CompleteDelivery() {
  var CompletedJob = ActiveJob;
  ActiveJob = null;
  document.getElementById('PreTrip').style.display = 'block';
  document.getElementById('DrivingView').style.display = 'none';
  ShowView('jobs');
  if (RoutingControl) { MapInstance.removeControl(RoutingControl); RoutingControl = null; }
  if (TrafficLayer) { MapInstance.removeLayer(TrafficLayer); TrafficLayer = null; }
  if (RouteLayer) { MapInstance.removeLayer(RouteLayer); RouteLayer = null; }
  AddChatMsg('Delivery completed successfully! Great work. Ready for your next load?', false);
  if (CompletedJob && CompletedJob.RealId && localStorage.getItem('wolfs_demo') !== 'true') {
    fetch(RelayUrl + '/api/jobs/' + CompletedJob.RealId, { method:'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ status:'completed', completedAt: new Date().toISOString() }) }).catch(function(){});
  }
}

var RoutingControl = null;
var TrafficLayer = null;

var UrbanZones = [
  {name:'Charlotte',lat:35.2271,lng:-80.8431,radius:0.15},
  {name:'Spartanburg',lat:34.9496,lng:-81.9320,radius:0.08},
  {name:'Greenville',lat:34.8526,lng:-82.3940,radius:0.12},
  {name:'Downtown LA',lat:34.0522,lng:-118.2437,radius:0.05},
  {name:'Long Beach',lat:33.7701,lng:-118.1937,radius:0.06}
];

