/* Wolf's Trucking Co. — Shared Demo Guide Component */
var DemoGuide = (function() {
  var Steps = [];
  var Current = 0;
  var Role = '';
  var Panel = null;
  var Highlight = null;
  var Active = false;
  var AutoMode = false;

  function IsDemo() { return localStorage.getItem('wolfs_demo') === 'true'; }

  function Init(role, steps) {
    Role = role;
    Steps = steps;
    Current = 0;
    Active = true;
    AutoMode = false;
    CreateDemoBadge();
    CreatePanel();
    CreateHighlight();
    BindKeys();
    Show();
  }

  function BindKeys() {
    if (window.__DemoKeysBound) return;
    window.__DemoKeysBound = true;
    window.addEventListener('keydown', function(E) {
      if (!Active) return;
      var Tag = (E.target && E.target.tagName) || '';
      if (Tag === 'INPUT' || Tag === 'TEXTAREA' || (E.target && E.target.isContentEditable)) return;
      if (E.key === 'ArrowRight' || E.key === ' ' || E.key === 'Enter') { E.preventDefault(); Next(); }
      else if (E.key === 'ArrowLeft') { E.preventDefault(); Prev(); }
      else if (E.key === 'Escape') { E.preventDefault(); ClearDemo(); End(); }
    });
    var ResizeTimer;
    window.addEventListener('resize', function() {
      if (!Active) return;
      clearTimeout(ResizeTimer);
      ResizeTimer = setTimeout(function() {
        if (!Active || Current >= Steps.length) return;
        HighlightTarget(Steps[Current].target);
      }, 120);
    });
    var ScrollTimer;
    window.addEventListener('scroll', function() {
      if (!Active) return;
      clearTimeout(ScrollTimer);
      ScrollTimer = setTimeout(function() {
        if (!Active || Current >= Steps.length) return;
        var El = PickVisible(Steps[Current].target);
        if (El) ApplyRectToHighlight(El);
      }, 60);
    }, { passive: true });
  }

  function CreateDemoBadge() {
    if (document.getElementById('DemoBadge')) return;
    var Badge = document.createElement('div');
    Badge.id = 'DemoBadge';
    Badge.style.cssText = 'position:fixed;top:8px;left:50%;transform:translateX(-50%);background:#ff6b35;color:#fff;padding:4px 14px;border-radius:0 0 10px 10px;font-size:.68rem;font-weight:800;letter-spacing:1px;z-index:9997;font-family:-apple-system,sans-serif;cursor:pointer;box-shadow:0 2px 8px rgba(0,0,0,.3)';
    Badge.textContent = 'DEMO MODE · ' + Role.toUpperCase() + ' · CLICK TO EXIT';
    Badge.onclick = function() { ClearDemo(); window.location.href = '/wolfstruckingco.com/'; };
    document.body.appendChild(Badge);
  }

  function CreatePanel() {
    if (Panel) Panel.remove();
    Panel = document.createElement('div');
    Panel.id = 'DemoPanel';
    Panel.innerHTML = '<div id="DemoPanelInner"></div>';
    document.body.appendChild(Panel);
    var s = Panel.style;
    s.position = 'fixed'; s.bottom = '20px'; s.right = '20px'; s.zIndex = '9999';
    s.width = '340px'; s.background = '#1a2332'; s.border = '2px solid #ff6b35';
    s.borderRadius = '16px'; s.boxShadow = '0 8px 32px rgba(0,0,0,.6)';
    s.fontFamily = "-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif";
    s.color = '#e8e8e8'; s.overflow = 'hidden'; s.transition = 'all .3s ease';
    s.cursor = 'default'; s.userSelect = 'none';
  }

  function CreateHighlight() {
    if (Highlight) Highlight.remove();
    Highlight = document.createElement('div');
    Highlight.id = 'DemoHighlight';
    document.body.appendChild(Highlight);
    var s = Highlight.style;
    s.position = 'fixed'; s.border = '3px solid #ff6b35'; s.borderRadius = '12px';
    s.zIndex = '9998'; s.pointerEvents = 'none'; s.display = 'none';
    s.boxShadow = '0 0 0 4000px rgba(0,0,0,.45)';
    s.transition = 'all .4s ease';
  }

  function Show() {
    if (!Panel || Current >= Steps.length) { End(); return; }
    var Step = Steps[Current];
    var Inner = document.getElementById('DemoPanelInner');
    Inner.innerHTML =
      '<div style="padding:16px 18px 12px;border-bottom:1px solid #2a3a4a;display:flex;align-items:center;justify-content:space-between">' +
        '<div style="display:flex;align-items:center;gap:8px">' +
          '<span style="background:#ff6b35;color:#fff;font-size:.7rem;font-weight:700;padding:3px 10px;border-radius:50px">DEMO</span>' +
          '<span style="font-size:.75rem;color:#8899aa">' + Role + '</span>' +
        '</div>' +
        '<div style="display:flex;align-items:center;gap:10px">' +
          '<span style="font-size:.75rem;color:#8899aa">' + (Current + 1) + ' / ' + Steps.length + '</span>' +
          '<button id="DemoExit" style="background:none;border:none;color:#8899aa;cursor:pointer;font-size:1.1rem;padding:2px 6px;line-height:1" title="Exit demo">&#10005;</button>' +
        '</div>' +
      '</div>' +
      '<div style="padding:16px 18px">' +
        '<div style="font-size:1.05rem;font-weight:700;margin-bottom:8px;color:#ff6b35">' + Step.title + '</div>' +
        '<div style="font-size:.88rem;color:#c8d0d8;line-height:1.55">' + Step.text + '</div>' +
      '</div>' +
      '<div style="padding:6px 18px 10px">' + ProgressBar() + '</div>' +
      '<div style="padding:0 18px 16px;display:flex;gap:10px;align-items:center">' +
        '<button id="DemoPrev" style="flex:0 0 auto;padding:10px 16px;border-radius:8px;border:1px solid #2a3a4a;background:#0f1419;color:#e8e8e8;font-size:.85rem;font-weight:600;cursor:pointer;min-height:40px;font-family:inherit;transition:border-color .2s' + (Current === 0 ? ';opacity:.3;pointer-events:none' : '') + '">&#8592; Back</button>' +
        '<button id="DemoNext" style="flex:1 1 auto;padding:10px 16px;border-radius:8px;border:none;background:#ff6b35;color:#fff;font-size:.9rem;font-weight:700;cursor:pointer;min-height:40px;font-family:inherit;transition:background .2s">' + (Current === Steps.length - 1 ? 'Finish &#10003;' : 'Next &#8594;') + '</button>' +
      '</div>';
    document.getElementById('DemoPrev').onclick = Prev;
    document.getElementById('DemoNext').onclick = Next;
    var ExitBtn = document.getElementById('DemoExit');
    if (ExitBtn) ExitBtn.onclick = function() { ClearDemo(); End(); window.location.href = '/wolfstruckingco.com/'; };
    if (!Step.target && Step.action) {
      try { Step.action(); } catch(E) {}
      Step.__consumed = true;
      setTimeout(function() { if (Highlight) { var El = FindPrimaryVisible(); if (El) { Highlight.style.display='block'; ApplyRectToHighlight(El); } } }, 80);
    } else {
      HighlightTarget(Step.target);
    }
    if (Step.action && AutoMode && !Step.__consumed) { setTimeout(function() { Step.action(); }, 600); }
  }

  function FindPrimaryVisible() {
    var Cands = ['.TabContent.Active', '.Tab.Active', '.View.Active', '#ActiveView.Show', '#KpisView:not([style*="display: none"])', '#JobsView:not([style*="display: none"])', '.MainContent', 'main', '#app'];
    for (var I = 0; I < Cands.length; I++) {
      try {
        var Nodes = document.querySelectorAll(Cands[I]);
        for (var J = 0; J < Nodes.length; J++) {
          var R = Nodes[J].getBoundingClientRect();
          if (R.width > 0 && R.height > 0 && R.right > 0 && R.left < window.innerWidth && R.bottom > 0 && R.top < window.innerHeight) {
            return Nodes[J];
          }
        }
      } catch(E) {}
    }
    return null;
  }

  function ProgressBar() {
    var Pct = Steps.length ? Math.round(((Current + 1) / Steps.length) * 100) : 0;
    return '<div style="height:5px;background:#0f1419;border-radius:99px;overflow:hidden">' +
      '<div style="height:100%;width:' + Pct + '%;background:linear-gradient(90deg,#22c55e,#ff6b35);transition:width .3s ease"></div>' +
      '</div>';
  }

  var RefreshTimer = null;
  function PickVisible(Sel) {
    if (typeof Sel !== 'string') return Sel;
    var All;
    try { All = document.querySelectorAll(Sel); } catch (E) { return null; }
    if (!All || !All.length) return null;
    for (var I = 0; I < All.length; I++) {
      var R = All[I].getBoundingClientRect();
      if (R.width > 0 && R.height > 0) return All[I];
    }
    return All[0];
  }

  function HighlightTarget(Sel) {
    if (!Sel || !Highlight) { if (Highlight) Highlight.style.display = 'none'; RepositionPanel(null); return; }
    var El = PickVisible(Sel);
    if (!El) { Highlight.style.display = 'none'; RepositionPanel(null); return; }
    Highlight.style.display = 'block';
    ApplyRectToHighlight(El);
    var R = El.getBoundingClientRect();
    if (R.top < 100 || R.bottom > window.innerHeight - 100) {
      El.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
    TrackRectFor(El, 800);
  }

  function ApplyRectToHighlight(El) {
    if (!Highlight || !El) return;
    var R = El.getBoundingClientRect();
    if (R.width === 0 && R.height === 0) return;
    Highlight.style.top = (R.top - 6) + 'px';
    Highlight.style.left = (R.left - 6) + 'px';
    Highlight.style.width = (R.width + 12) + 'px';
    Highlight.style.height = (R.height + 12) + 'px';
    RepositionPanel(R);
  }

  function TrackRectFor(El, DurationMs) {
    if (RefreshTimer) { cancelAnimationFrame(RefreshTimer); RefreshTimer = null; }
    var Start = Date.now();
    var LastJson = '';
    function Step() {
      if (!Active) return;
      if (Current >= Steps.length) return;
      if (!El || !document.body.contains(El)) return;
      var R = El.getBoundingClientRect();
      var Key = R.top + ',' + R.left + ',' + R.width + ',' + R.height;
      if (Key !== LastJson) { LastJson = Key; ApplyRectToHighlight(El); }
      if (Date.now() - Start < DurationMs) RefreshTimer = requestAnimationFrame(Step);
      else RefreshTimer = null;
    }
    RefreshTimer = requestAnimationFrame(Step);
  }

  function RepositionPanel(TargetRect) {
    if (!Panel) return;
    var Pad = 20, BadgeGap = 44;
    var Pr = Panel.getBoundingClientRect();
    var W = Pr.width || 340, H = Pr.height || 240;
    var Vw = window.innerWidth, Vh = window.innerHeight;
    function Corner(Right, Bottom) {
      return {
        top: Bottom ? '' : BadgeGap + 'px',
        bottom: Bottom ? Pad + 'px' : '',
        left: Right ? '' : Pad + 'px',
        right: Right ? Pad + 'px' : '',
        x: Right ? Vw - W - Pad : Pad,
        y: Bottom ? Vh - H - Pad : BadgeGap
      };
    }
    function Overlaps(C, R) {
      return !(C.x + W < R.left || C.x > R.right || C.y + H < R.top || C.y > R.bottom);
    }
    var Best = Corner(true, true);
    if (TargetRect && (TargetRect.width > 0 || TargetRect.height > 0)) {
      var Cx = (TargetRect.left + TargetRect.right) / 2;
      var Cy = (TargetRect.top + TargetRect.bottom) / 2;
      var Preferred = Corner(Cx <= Vw / 2, Cy <= Vh / 2);
      if (!Overlaps(Preferred, TargetRect)) Best = Preferred;
      else {
        var Ordered = [
          Preferred,
          Corner(Cx <= Vw / 2, Cy > Vh / 2),
          Corner(Cx > Vw / 2, Cy <= Vh / 2),
          Corner(Cx > Vw / 2, Cy > Vh / 2)
        ];
        for (var I = 0; I < Ordered.length; I++) {
          if (!Overlaps(Ordered[I], TargetRect)) { Best = Ordered[I]; break; }
        }
      }
    }
    Panel.style.top = Best.top; Panel.style.right = Best.right;
    Panel.style.bottom = Best.bottom; Panel.style.left = Best.left;
  }

  function Next() {
    var Step = Steps[Current];
    if (Step.action && !Step.__consumed) { try { Step.action(); } catch(E) {} }
    Step.__consumed = false;
    Current++;
    if (Current >= Steps.length) { End(); return; }
    Show();
  }

  function Prev() {
    if (Current > 0) { Current--; Show(); }
  }

  function End() {
    Active = false;
    if (Panel) Panel.remove();
    if (Highlight) { Highlight.style.display = 'none'; Highlight.remove(); }
    var Badge = document.getElementById('DemoBadge');
    if (Badge) Badge.remove();
    Panel = null; Highlight = null;
    ShowCompletionModal();
  }

  function ShowCompletionModal() {
    var Overlay = document.createElement('div');
    Overlay.style.cssText = 'position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,.75);z-index:10000;display:flex;align-items:center;justify-content:center;padding:20px';
    Overlay.innerHTML =
      '<div style="background:#1a2332;border:2px solid #ff6b35;border-radius:16px;padding:36px 32px;max-width:480px;width:100%;text-align:center;font-family:-apple-system,BlinkMacSystemFont,\'Segoe UI\',Roboto,sans-serif;color:#e8e8e8">' +
        '<div style="font-size:3rem;margin-bottom:12px">&#127881;</div>' +
        '<h2 style="font-size:1.4rem;font-weight:800;margin-bottom:8px;color:#ff6b35">Demo Complete!</h2>' +
        '<p style="font-size:.95rem;color:#8899aa;line-height:1.6;margin-bottom:24px">You have seen the full ' + Role + ' experience. Everything here works the same in real business mode — all data persists to Cloudflare R2.</p>' +
        '<div style="display:flex;flex-direction:column;gap:10px">' +
          '<button id="DemoTryAnother" style="padding:14px 24px;border-radius:10px;background:#ff6b35;border:none;color:#fff;font-size:.95rem;font-weight:700;cursor:pointer;min-height:48px;font-family:inherit">Try Another Role</button>' +
          '<button id="DemoRestart" style="padding:12px 24px;border-radius:10px;background:transparent;border:1px solid #2a3a4a;color:#e8e8e8;font-size:.9rem;font-weight:600;cursor:pointer;min-height:44px;font-family:inherit">Restart This Demo</button>' +
          '<button id="DemoSignIn" style="padding:12px 24px;border-radius:10px;background:transparent;border:1px solid #2a3a4a;color:#8899aa;font-size:.9rem;font-weight:600;cursor:pointer;min-height:44px;font-family:inherit">Sign In for Real</button>' +
          '<button id="DemoHome" style="padding:12px 24px;border-radius:10px;background:transparent;border:none;color:#8899aa;font-size:.85rem;cursor:pointer;min-height:40px;font-family:inherit">Back to Homepage</button>' +
        '</div>' +
      '</div>';
    document.body.appendChild(Overlay);
    document.getElementById('DemoTryAnother').onclick = function() { Overlay.remove(); ClearDemo(); window.location.href = '/wolfstruckingco.com/Login/#demo'; };
    document.getElementById('DemoRestart').onclick = function() { Overlay.remove(); Current = 0; Active = true; CreatePanel(); CreateHighlight(); Show(); };
    document.getElementById('DemoSignIn').onclick = function() { Overlay.remove(); ClearDemo(); window.location.href = '/wolfstruckingco.com/Login/'; };
    document.getElementById('DemoHome').onclick = function() { Overlay.remove(); ClearDemo(); window.location.href = '/wolfstruckingco.com/'; };
  }

  function SetDemoSession(role, email, name) {
    localStorage.setItem('wolfs_demo', 'true');
    localStorage.setItem('wolfs_session', 'demo_' + Date.now());
    localStorage.setItem('wolfs_email', email);
    localStorage.setItem('wolfs_role', role);
    localStorage.setItem('wolfs_name', name);
  }

  function ClearDemo() {
    localStorage.removeItem('wolfs_demo');
    localStorage.removeItem('wolfs_session');
    localStorage.removeItem('wolfs_email');
    localStorage.removeItem('wolfs_role');
    localStorage.removeItem('wolfs_name');
  }

  return {
    Init: Init, IsDemo: IsDemo, End: End, Show: Show,
    SetDemoSession: SetDemoSession, ClearDemo: ClearDemo,
    Next: Next, Prev: Prev
  };
})();
