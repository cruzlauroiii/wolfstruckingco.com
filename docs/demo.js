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
    CreatePanel();
    CreateHighlight();
    Show();
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
        '<span style="font-size:.75rem;color:#8899aa">' + (Current + 1) + ' / ' + Steps.length + '</span>' +
      '</div>' +
      '<div style="padding:16px 18px">' +
        '<div style="font-size:1.05rem;font-weight:700;margin-bottom:8px;color:#ff6b35">' + Step.title + '</div>' +
        '<div style="font-size:.88rem;color:#c8d0d8;line-height:1.55">' + Step.text + '</div>' +
      '</div>' +
      '<div style="padding:10px 18px 16px;display:flex;gap:8px;justify-content:space-between;align-items:center">' +
        '<button id="DemoPrev" style="padding:10px 20px;border-radius:8px;border:1px solid #2a3a4a;background:#0f1419;color:#e8e8e8;font-size:.85rem;font-weight:600;cursor:pointer;min-height:40px;font-family:inherit;transition:border-color .2s' + (Current === 0 ? ';opacity:.3;pointer-events:none' : '') + '">&#8592; Back</button>' +
        '<div style="display:flex;gap:4px">' + Dots() + '</div>' +
        '<button id="DemoNext" style="padding:10px 20px;border-radius:8px;border:none;background:#ff6b35;color:#fff;font-size:.85rem;font-weight:700;cursor:pointer;min-height:40px;font-family:inherit;transition:background .2s">' + (Current === Steps.length - 1 ? 'Finish' : 'Next &#8594;') + '</button>' +
      '</div>';
    document.getElementById('DemoPrev').onclick = Prev;
    document.getElementById('DemoNext').onclick = Next;
    HighlightTarget(Step.target);
    if (Step.action && AutoMode) { setTimeout(function() { Step.action(); }, 600); }
  }

  function Dots() {
    var H = '';
    for (var I = 0; I < Steps.length; I++) {
      var C = I === Current ? '#ff6b35' : I < Current ? '#22c55e' : '#2a3a4a';
      H += '<div style="width:6px;height:6px;border-radius:50%;background:' + C + '"></div>';
    }
    return H;
  }

  function HighlightTarget(Sel) {
    if (!Sel || !Highlight) { Highlight.style.display = 'none'; return; }
    var El = typeof Sel === 'string' ? document.querySelector(Sel) : Sel;
    if (!El) { Highlight.style.display = 'none'; return; }
    var R = El.getBoundingClientRect();
    Highlight.style.display = 'block';
    Highlight.style.top = (R.top - 6) + 'px';
    Highlight.style.left = (R.left - 6) + 'px';
    Highlight.style.width = (R.width + 12) + 'px';
    Highlight.style.height = (R.height + 12) + 'px';
    if (R.top < 100 || R.bottom > window.innerHeight - 100) {
      El.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  function Next() {
    var Step = Steps[Current];
    if (Step.action) { try { Step.action(); } catch(E) {} }
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
    Panel = null; Highlight = null;
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
