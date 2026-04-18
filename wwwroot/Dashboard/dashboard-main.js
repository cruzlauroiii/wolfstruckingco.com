function LoadAuditTrail(Val) {
  var Container = document.getElementById('ChatMessages');
  if (Val === 'current') {
    Container.innerHTML = '<div class="ChatMsg ChatMsgBot">Dispatch AI ready. How can I help you today?</div>';
    return;
  }
  Container.innerHTML =
    '<div class="ChatMsg ChatMsgBot">Audit trail for ' + Val + '</div>' +
    '<div class="ChatMsg ChatMsgUser">Heading to pickup now</div>' +
    '<div class="ChatMsg ChatMsgBot">Roger that. ETA to pickup is 22 minutes via I-5 South. Traffic is clear.</div>' +
    '<div class="ChatMsg ChatMsgUser">Arrived at pickup. Loading now.</div>' +
    '<div class="ChatMsg ChatMsgBot">Confirmed. Seal number recorded. Estimated delivery time 2:15 PM.</div>' +
    '<div class="ChatMsg ChatMsgUser">Load complete, departing.</div>' +
    '<div class="ChatMsg ChatMsgBot">Route updated. Take I-710 South to avoid construction on I-5.</div>' +
    '<div class="ChatMsg ChatMsgUser">Delivered. Got signature.</div>' +
    '<div class="ChatMsg ChatMsgBot">Delivery confirmed at 2:08 PM. On-time. Great work!</div>';
}

function ToggleChatVoice() {
  if (ChatVoiceActive) {
    ChatVoiceActive = false;
    document.getElementById('ChatVoiceBtn').style.background = 'var(--success)';
    if (ChatSpeechRec) { try { ChatSpeechRec.stop(); } catch(E){} ChatSpeechRec = null; }
  } else {
    ChatVoiceActive = true;
    document.getElementById('ChatVoiceBtn').style.background = '#ef4444';
    var SpeechApi = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechApi) return;
    ChatSpeechRec = new SpeechApi();
    ChatSpeechRec.continuous = true;
    ChatSpeechRec.interimResults = false;
    ChatSpeechRec.lang = 'en-US';
    ChatSpeechRec.onresult = function(E) {
      var Last = E.results[E.results.length - 1];
      if (Last.isFinal) {
        var T = Last[0].transcript.trim();
        if (T) {
          AddChatMsg(T, true);
          SendToRelay({type:'chat',content:T,id:'chat_'+Date.now()});
        }
      }
    };
    ChatSpeechRec.onend = function() { if (ChatVoiceActive) try { ChatSpeechRec.start(); } catch(E){} };
    ChatSpeechRec.onerror = function() {};
    try { ChatSpeechRec.start(); } catch(E){}
  }
}

function PollRelay() {
  fetch(RelayUrl + '/poll?role=client')
    .then(function(R) { return R.json(); })
    .then(function(Data) {
      if (Data.messages) {
        Data.messages.forEach(function(Raw) {
          try {
            var Msg = JSON.parse(Raw);
            var Text = Msg.content || Msg.Content || '';
            if (!Text) return;
            AddChatMsg(Text, false);
            if (ChatVoiceActive && window.speechSynthesis) {
              var U = new SpeechSynthesisUtterance(Text);
              U.lang = 'en-US';
              window.speechSynthesis.speak(U);
            }
          } catch(E) {}
        });
      }
    })
    .catch(function() {})
    .finally(function() { setTimeout(PollRelay, 2000); });
}

function Logout() {
  localStorage.removeItem('wolfs_session');
  localStorage.removeItem('wolfs_email');
  localStorage.removeItem('wolfs_role');
  localStorage.removeItem('wolfs_onboarded');
  window.location.href = '/wolfstruckingco.com/Login/';
}

var OnboardSteps = [
  {Title:'Job Board',Text:'Available deliveries appear in the bottom panel. Each card shows the pickup, delivery, pay, distance, and time window.',Target:'BottomPanel'},
  {Title:'Job Details',Text:'Tap any job card to see full details including weight, load type, and special instructions. Hit Accept to take the job.',Target:'JobList'},
  {Title:'Pre-Trip Checklist',Text:'Before every delivery, you must complete the pre-trip inspection checklist. This is a DOT requirement and keeps everyone safe.',Target:'BottomPanel'},
  {Title:'Route Navigation',Text:'Once driving, the map shows your route with turn-by-turn markers. Follow the orange dashed line to your destination.',Target:'Map'},
  {Title:'Dispatch Chat',Text:'Use the chat button to communicate with AI dispatch. Get route updates, report issues, or ask questions anytime. Voice calls are supported too.',Target:'BtnChat'},
  {Title:'You Are All Set!',Text:'Your dashboard is ready. Check the job board for available deliveries and accept your first job. Drive safe out there!',Target:null}
];
var OnboardIndex = 0;

function CheckOnboarding() {
  // Onboarding tour disabled — drivers land directly on the dashboard.
  try { localStorage.setItem('wolfs_onboarded', 'true'); } catch (_) {}
}

function ShowOnboardStep() {
  if (OnboardIndex >= OnboardSteps.length) {
    document.getElementById('Onboarding').classList.remove('Active');
    document.getElementById('Highlight').style.display = 'none';
    localStorage.setItem('wolfs_onboarded', 'true');
    return;
  }
  var Step = OnboardSteps[OnboardIndex];
  document.getElementById('Onboarding').classList.add('Active');
  var Dots = '';
  for (var I = 0; I < OnboardSteps.length; I++) {
    Dots += '<div class="OnboardDot' + (I === OnboardIndex ? ' Active' : '') + '"></div>';
  }
  document.getElementById('OnboardContent').innerHTML =
    '<h3>' + Step.Title + '</h3>' +
    '<p>' + Step.Text + '</p>' +
    '<div class="OnboardProgress">' + Dots + '</div>' +
    '<button class="BtnNext" onclick="NextOnboardStep()">' + (OnboardIndex === OnboardSteps.length - 1 ? 'Get Started' : 'Next') + '</button>';
  if (Step.Target) {
    var El = document.getElementById(Step.Target);
    if (El) {
      var Rect = El.getBoundingClientRect();
      var Hl = document.getElementById('Highlight');
      Hl.style.display = 'block';
      Hl.style.top = Rect.top - 4 + 'px';
      Hl.style.left = Rect.left - 4 + 'px';
      Hl.style.width = Rect.width + 8 + 'px';
      Hl.style.height = Rect.height + 8 + 'px';
    }
  } else {
    document.getElementById('Highlight').style.display = 'none';
  }
}

function NextOnboardStep() {
  OnboardIndex++;
  ShowOnboardStep();
}

InitMap();
RenderJobs();
PollRelay();

// Fetch real jobs from R2 in non-demo mode
function FetchR2Jobs() {
  if (localStorage.getItem('wolfs_demo') === 'true') return;
  fetch(RelayUrl + '/api/jobs?status=available').then(function(R){return R.json()}).then(function(Data){
    if (!Data.items || Data.items.length === 0) return;
    var R2Jobs = Data.items.map(function(J, Idx) {
      return {
        Id: 1000 + Idx,
        RealId: J.id,
        Title: J.title || 'Shipment',
        Pickup: J.pickup || '',
        Delivery: J.delivery || '',
        Pay: typeof J.pay === 'number' ? J.pay : parseFloat((J.pay||'0').replace(/[$,]/g, '')) || 0,
        Distance: (J.distance || 0) + ' mi',
        Time: J.duration || '2-3 hrs',
        Weight: J.weight || '',
        Type: J.type || 'FTL',
        Window: J.window || 'Flexible',
        Notes: J.cargo || J.instructions || ''
      };
    });
    // Prepend real jobs to hardcoded ones
    Jobs = R2Jobs.concat(Jobs);
    RenderJobs();
  }).catch(function(){});
}
setInterval(FetchR2Jobs, 15000);
FetchR2Jobs();

if (typeof DemoGuide !== 'undefined' && DemoGuide.IsDemo()) {
  setTimeout(function() {
    DemoGuide.Init('Driver', [
      {title:'1. Driver Dashboard', text:'Welcome Lauro! Complete dispatch center: live map, job panel, active delivery tracker, dispatch chat, KPI badges. Everything real-time, persisted to R2.', target:'#Map'},
      {title:'2. Identity + KPIs', text:'Name, on-time rate (98.2%), earnings today ($412) in top bar. Updates as you complete deliveries.', target:'.DriverInfo'},
      {title:'3. Burger Menu', text:'Side menu access: Available Jobs, Active Delivery, My KPIs, Dispatch Chat, Settings, Sign Out. One tap from anywhere.', target:'.BtnBurger', action:function(){ ToggleMenu(); }},
      {title:'4. Side Menu Items', text:'Menu scrolls on small screens. Each item switches the panel view. Sign Out clears session.', target:'#SideMenu .MenuItems', action:function(){ ToggleMenu(); }},
      {title:'5. Available Jobs', text:'Left panel shows 6 open loads. Pay ($165-$340), pickup/delivery dots (green/orange), distance, time window, cargo type.', target:'#JobList'},
      {title:'6. Job Types', text:'All freight types: Container Drayage (Port of LA), LTL multi-stop, Dedicated FTL (Walmart), Reefer produce, Flatbed construction, Express medical.', target:'#JobList'},
      {title:'7. Expand Details', text:'Click a job for full details: weight, load type, time window, special instructions (TWIC, liftgate, temperature, appointments, tarping).', target:'.JobCard', action:function(){ ToggleJobDetails(1); }},
      {title:'8. Accept Job', text:'Click "Accept Job" — creates delivery record, logs audit, locks load to you. Persisted to R2.', target:'.JobCard', action:function(){ AcceptJob(1); }},
      {title:'9. Pre-Trip Inspection', text:'Federal law (49 CFR 392.7): 8-point pre-trip before every delivery. Oil, tires, brakes, lights, mirrors, coupling, cargo, paperwork.', target:'#ChecklistItems'},
      {title:'10. Complete Checklist', text:'Tap each checkbox. CANNOT start driving until all 8 pass. Any fail = truck parked until repair.', target:'#ChecklistItems', action:function(){ for(var i=0;i<ChecklistItems.length;i++){var el=document.getElementById('Check_'+i);if(el&&!el.classList.contains('Checked'))ToggleCheck(i);} }},
      {title:'11. Live Traffic Route', text:'Map draws OSRM route with traffic colors: green=clear, yellow=moderate, red=heavy. Avoids urban congestion zones automatically.', target:'#Map'},
      {title:'12. Start Driving', text:'Click "Start Driving" — delivery begins, dispatch notified, clock starts, GPS tracking active.', target:'#BtnStartDrive', action:function(){ StartDriving(); }},
      {title:'13. 9-Step Delivery', text:'Workflow: depart → pickup → load → verify seal/BOL → depart → route → arrive delivery → unload → signature. Each logged to audit trail.', target:'#DeliverySteps'},
      {title:'14. Dispatch Chat', text:'Message dispatch AI. Ask routes, report delays, request help. Voice supported. All conversations saved.', target:'#BtnChat', action:function(){ OpenChat(); }},
      {title:'15. Past Load History', text:'Dropdown shows past dispatch conversations per load. Every message compliance-logged.', target:'#AuditSelect'},
      {title:'16. Voice Chat', text:'Microphone enables hands-free chat. Dispatch replies read aloud via speech synthesis. Perfect for highway.', target:'#ChatVoiceBtn', action:function(){ CloseChat(); }},
      {title:'17. Complete Delivery', text:'After unload + signature, click Complete. Updates status, timestamps, triggers payment. You earn full pay.', target:'#BtnCompleteDelivery'},
      {title:'18. My KPIs', text:'Personal dashboard: on-time rate, weekly earnings ($4,821), monthly loads (156), safety score (4.9/5), delivery history.', target:null, action:function(){ ShowView('kpis'); }},
      {title:'19. Driver Earnings', text:'Pay per load, not hourly. Regional $275-$562, Express $165+. Drivers earn $65K-$80K/year with overtime.', target:'#KpiContent'},
      {title:'20. Settings', text:'Update CDL info, truck unit, home base, phone, preferences. All persists to R2 for real business.', target:null},
      {title:'Driver Demo Complete!', text:'ALL 20 features shown: jobs, details, acceptance, pre-trip, routing, driving, steps, chat, voice, KPIs, earnings. Ready to earn money!', target:null, action:function(){ ShowView('jobs'); }}
    ]);
  }, 800);
} else {
  setTimeout(CheckOnboarding, 1000);
}
