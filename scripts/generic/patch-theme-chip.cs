#:property TargetFramework=net11.0

// patch-theme-chip.cs - rewire ThemeChip to a pure HTML button that calls
// window.WolfsInterop.themeWrite directly (item #12). Works pre-hydration
// on static prerendered pages. Also adds dark/light CSS rules to app.css
// keyed off the [data-theme] attribute that themeWrite sets on <html>.
const string Razor = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ThemeChip.razor";
const string Css = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\wwwroot\css\app.css";

var Click = "var w=window.WolfsInterop||{};var cur=(w.themeRead?w.themeRead():(localStorage.getItem('wolfs_theme')||'auto'));var next=cur==='auto'?'dark':(cur==='dark'?'light':'auto');try{localStorage.setItem('wolfs_theme',next);document.documentElement.setAttribute('data-theme',next==='auto'?'':next);}catch(e){}var lbl=next==='dark'?'🌙 Dark':(next==='light'?'☀ Light':'🌗 Auto');this.textContent=lbl;";
var ThemeChipNew = "@* Reusable dark/light/auto theme cycler. *@\n" +
                   "<button type=\"button\" class=\"wt-theme-chip\" title=\"Switch theme\" onclick=\"" + Click + "\">🌗 Auto</button>\n" +
                   "<script>(function(){try{var t=localStorage.getItem('wolfs_theme')||'auto';if(t!=='auto')document.documentElement.setAttribute('data-theme',t);var btns=document.querySelectorAll('.wt-theme-chip');for(var i=0;i<btns.length;i++){btns[i].textContent=t==='dark'?'🌙 Dark':(t==='light'?'☀ Light':'🌗 Auto');}}catch(e){}})();</script>\n";

await File.WriteAllTextAsync(Razor, ThemeChipNew.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
await Console.Out.WriteLineAsync($"wrote {Razor}");

var C = await File.ReadAllTextAsync(Css);
const string DarkRule = "html[data-theme=\"dark\"]";
if (C.Contains(DarkRule, StringComparison.Ordinal)) { await Console.Out.WriteLineAsync("css already has dark rules"); return 0; }
var Add = "html[data-theme=\"dark\"]{--bg:#0a0d12;--card:#1e293b;--text:#f8fafc;--text-muted:#cbd5e1;--border:#334155;--accent:#fb923c;--accent-hover:#ea580c}html[data-theme=\"dark\"] .TopBar{background:#0f172a}html[data-theme=\"dark\"] .Field input,html[data-theme=\"dark\"] .Field textarea,html[data-theme=\"dark\"] .Field select,html[data-theme=\"dark\"] .CredForm input{background:#1e293b;color:#f8fafc}html[data-theme=\"dark\"] .Listing,html[data-theme=\"dark\"] .Card,html[data-theme=\"dark\"] .Stat,html[data-theme=\"dark\"] .ModalBody,html[data-theme=\"dark\"] .LoginCard{background:#1e293b;color:#f8fafc}html[data-theme=\"light\"]{--bg:#ffffff;--card:#f3f6fa;--text:#0a0d12;--text-muted:#334155;--border:#cbd5e1}";
await File.WriteAllTextAsync(Css, C + Add);
await Console.Out.WriteLineAsync($"appended dark/light theme rules to {Css}");
return 0;
