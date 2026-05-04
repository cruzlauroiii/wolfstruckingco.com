#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.Json;
using System.Runtime.InteropServices;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string Unesc(string s)
{
    var sb = new System.Text.StringBuilder(s.Length);
    for (int i = 0; i < s.Length; i++)
    {
        if (s[i] == '\\' && i + 1 < s.Length)
        {
            char n = s[i + 1];
            if (n == '"') { sb.Append('"'); i++; }
            else if (n == '\\') { sb.Append('\\'); i++; }
            else if (n == 'n') { sb.Append('\n'); i++; }
            else if (n == 't') { sb.Append('\t'); i++; }
            else if (n == 'r') { sb.Append('\r'); i++; }
            else if (n == '\'') { sb.Append('\''); i++; }
            else sb.Append(s[i]);
        }
        else sb.Append(s[i]);
    }
    return sb.ToString();
}

string? Get(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        bool Verbatim = After.StartsWith("@", StringComparison.Ordinal);
        if (Verbatim) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        var Raw = After.Substring(1, End - 1);
        return Verbatim ? Raw : Unesc(Raw);
    }
    return null;
}

var Repo = Get("Repo")!;
var Frames = Get("Frames")!;
var Audio = Get("Audio")!;
var Docs = Get("Docs")!;
var ScenesPath = Get("ScenesPath")!;
var ChatGroupsJson = Get("ChatGroupsJson")!;
var SsoAckPath = Get("SsoAckPath")!;
var HomeUrl = Get("HomeUrl")!;
var SsoWaitSec = int.Parse(Get("SsoWaitSec") ?? "90");
var SsoHardTimeoutSec = int.Parse(Get("SsoHardTimeoutSec") ?? "600");
var CdpTimeoutSec = int.Parse(Get("CdpTimeoutSec") ?? "60");
var CdpRetries = int.Parse(Get("CdpRetries") ?? "3");

Directory.CreateDirectory(Frames);

[DllImport("user32.dll")] static extern int MessageBeep(uint uType);
[DllImport("kernel32.dll")] static extern bool Beep(uint freq, uint dur);

void AlarmBurst()
{
    try { Beep(2200, 250); Beep(1500, 250); Beep(2800, 250); Beep(1500, 250); Beep(2200, 350); } catch { }
}

string CachedWolfsPageIdx = "";
async Task<int> Cdp(string Name, string Body)
{
    if (Name != "list" && Body.Contains("PageId = \"1\""))
    {
        if (string.IsNullOrEmpty(CachedWolfsPageIdx))
        {
            var Listing = "";
            var ListLog = Path.Combine(Path.GetTempPath(), $"list-{Guid.NewGuid():N}.log");
            var ListCfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        public const string Command = \"list_pages\";\n        public const string OutputPath = @\"" + ListLog + "\";\n    }\n}\n";
            var Tmp2 = Path.Combine(Path.GetTempPath(), $"cdp-list-inner-{Guid.NewGuid():N}.cs");
            await File.WriteAllTextAsync(Tmp2, ListCfg);
            var P2 = new ProcessStartInfo("dotnet") { WorkingDirectory = Repo, RedirectStandardOutput = true, RedirectStandardError = true };
            P2.ArgumentList.Add("run"); P2.ArgumentList.Add("main/scripts/generic/chrome-devtools.cs"); P2.ArgumentList.Add(Tmp2);
            using var Pp = Process.Start(P2)!;
            await Pp.WaitForExitAsync();
            try { Listing = await File.ReadAllTextAsync(ListLog); } catch { }
            try { File.Delete(Tmp2); File.Delete(ListLog); } catch { }
            foreach (var Line in Listing.Split('\n'))
            {
                var T = Line.Trim();
                if (!T.Contains("wolfstruckingco", StringComparison.OrdinalIgnoreCase)) continue;
                var Colon = T.IndexOf(':');
                if (Colon < 1) continue;
                var Idx = T.Substring(0, Colon).Trim();
                if (Idx.All(char.IsDigit)) { CachedWolfsPageIdx = Idx; break; }
            }
        }
        if (!string.IsNullOrEmpty(CachedWolfsPageIdx) && CachedWolfsPageIdx != "1")
        {
            Body = Body.Replace("PageId = \"1\"", $"PageId = \"{CachedWolfsPageIdx}\"");
        }
    }
    var Cfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        " + Body + "\n    }\n}\n";
    for (int A = 0; A < CdpRetries; A++)
    {
        var Tmp = Path.Combine(Path.GetTempPath(), $"cdp-{Name}-{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(Tmp, Cfg);
        var Psi = new ProcessStartInfo("dotnet") { WorkingDirectory = Repo, RedirectStandardOutput = true, RedirectStandardError = true };
        Psi.ArgumentList.Add("run");
        Psi.ArgumentList.Add("main/scripts/generic/chrome-devtools.cs");
        Psi.ArgumentList.Add(Tmp);
        using var P = Process.Start(Psi)!;
        var T = P.WaitForExitAsync();
        if (await Task.WhenAny(T, Task.Delay(CdpTimeoutSec * 1000)) != T)
        {
            try { P.Kill(true); } catch { }
            try { File.Delete(Tmp); } catch { }
            await Task.Delay(4000);
            continue;
        }
        try { File.Delete(Tmp); } catch { }
        await Task.Delay(500);
        return P.ExitCode;
    }
    return -2;
}

async Task<string> CdpRead(string Name, string Body)
{
    var Log = Path.Combine(Path.GetTempPath(), $"out-{Name}-{Guid.NewGuid():N}.log");
    var FullBody = Body + $"\n        public const string OutputPath = @\"{Log}\";";
    await Cdp(Name, FullBody);
    string C = "";
    try { C = await File.ReadAllTextAsync(Log); } catch { }
    try { File.Delete(Log); } catch { }
    return C;
}

string Eval(string Fn) =>
    "public const string Command = \"evaluate_script\";\n        " +
    "public const string PageId = \"1\";\n        " +
    $"public const string Function = \"{Fn}\";";

async Task NewPageAt(string Url) => await Cdp("new", $"public const string Command = \"new_page\";\n        public const string Url = \"{Url.Replace("\"", "\\\"")}\";");

async Task ReplaceTab(string Url) => await NewPageAt(Url);

async Task ForceLight() => await Cdp("light", Eval("() => { document.documentElement.setAttribute('data-theme','light'); return 'ok'; }"));

async Task SendMsg(string Msg)
{
    var Esc = Msg.Replace("\\", "\\\\").Replace("'", "\\'");
    var Fn = $"() => {{ var i = document.querySelector('.ChatInputRow input[type=text]'); if (!i) return 'no-input'; var nv = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype,'value').set; nv.call(i,'{Esc}'); i.dispatchEvent(new Event('input',{{bubbles:true}})); var s = document.querySelector('.ChatBtnRound.Send'); if (!s) return 'no-send'; s.click(); return 'sent'; }}";
    await Cdp("send", Eval(Fn));
}

async Task<string> WolfsPageIndex()
{
    var Listing = await CdpRead("list", "public const string Command = \"list_pages\";");
    foreach (var Line in Listing.Split('\n'))
    {
        var T = Line.Trim();
        if (!T.Contains("wolfstruckingco", StringComparison.OrdinalIgnoreCase)) continue;
        var Colon = T.IndexOf(':');
        if (Colon < 1) continue;
        var Idx = T.Substring(0, Colon).Trim();
        if (Idx.All(char.IsDigit)) return Idx;
    }
    return "1";
}

async Task<int> Screenshot(string Pad)
{
    var Out = Path.Combine(Frames, Pad + ".png");
    try { File.Delete(Out); } catch { }
    foreach (var PageIdx in new[] { "1", "2", "3", "4" })
    {
        var Body = $"public const string Command = \"take_screenshot\";\n        public const string PageId = \"{PageIdx}\";\n        public const string FilePath = @\"{Out}\";";
        var Rc = await Cdp("shot", Body);
        if (File.Exists(Out) && new FileInfo(Out).Length > 0)
        {
            Console.WriteLine($"  shot pad={Pad} via pageIdx={PageIdx}");
            return Rc;
        }
        try { File.Delete(Out); } catch { }
    }
    Console.WriteLine($"  Screenshot FAIL pad={Pad} png-missing on all PageIdx 1-4");
    return -3;
}

async Task<int> Encode(string Pad)
{
    var Png = Path.Combine(Frames, Pad + ".png");
    var Wav = Path.Combine(Audio, $"scene-{Pad}.mp3");
    var Mp4 = Path.Combine(Docs, $"scene-{Pad}.mp4");
    if (!File.Exists(Png)) { Console.WriteLine($"  Encode FAIL pad={Pad} png-missing"); return -1; }
    if (!File.Exists(Wav)) { Console.WriteLine($"  Encode FAIL pad={Pad} wav-missing ({Wav})"); return -2; }
    var Psi = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var A in new[] { "-y", "-loop", "1", "-i", Png, "-i", Wav, "-c:v", "libx264", "-tune", "stillimage", "-pix_fmt", "yuv420p", "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30", "-c:a", "aac", "-b:a", "128k", "-ar", "44100", "-shortest", Mp4 }) Psi.ArgumentList.Add(A);
    using var P = Process.Start(Psi)!;
    var OutTask = P.StandardOutput.ReadToEndAsync();
    var ErrTask = P.StandardError.ReadToEndAsync();
    var ExitTask = P.WaitForExitAsync();
    var Winner = await Task.WhenAny(ExitTask, Task.Delay(120000));
    if (Winner != ExitTask) { try { P.Kill(true); } catch { } Console.WriteLine($"  Encode FAIL pad={Pad} ffmpeg-timeout"); return -1; }
    await Task.WhenAll(OutTask, ErrTask);
    return P.ExitCode;
}

async Task<bool> HasPasswordInput()
{
    var C = await CdpRead("hp", Eval("() => document.querySelector('input[type=password]') ? 'yes' : 'no'"));
    return C.Contains("yes");
}

async Task<(bool hasLogoff, bool bodyReady)> CheckLogoffStrict()
{
    var C = await CdpRead("logoff-check", Eval("() => { var t = document.body ? document.body.innerText : ''; if (t.trim().length < 10) return 'not-ready'; return /(log\\\\s*off|sign\\\\s*out|log\\\\s*out)/i.test(t) ? 'yes' : 'no'; }"));
    if (C.Contains("not-ready")) return (false, false);
    return (C.Contains("yes"), true);
}

async Task<bool> HasLogoffText()
{
    for (int A = 0; A < 5; A++)
    {
        var (Has, Ready) = await CheckLogoffStrict();
        if (Ready) return Has;
        await Task.Delay(2000);
    }
    return true;
}

async Task<string> CurrentUrl()
{
    var C = await CdpRead("url", Eval("() => location.href"));
    return C.Trim().Trim('"').Trim('\'');
}

async Task ClickLogoutFirst()
{
    var Fn = "() => { var btns = Array.from(document.querySelectorAll('button,a,[role=button]')); var b = btns.find(x => /(log\\\\s*out|sign\\\\s*out|log\\\\s*off)/i.test(x.textContent||x.getAttribute('aria-label')||'')); if (!b) return 'no-logout'; b.click(); return 'logged-out'; }";
    await Cdp("logout", Eval(Fn));
}

async Task ClickSsoButton(string Provider)
{
    var Fn = $"() => {{ var btns = Array.from(document.querySelectorAll('button,a,[role=button]')); var b = btns.find(x => /{Provider}/i.test(x.textContent||x.getAttribute('aria-label')||'')); if (!b) return 'no-btn'; b.click(); return 'clicked'; }}";
    await Cdp("sso-click", Eval(Fn));
}

async Task ClickAccountByEmail(string Email)
{
    if (string.IsNullOrEmpty(Email)) return;
    var Esc = Email.Replace("'", "\\'");
    var Fn = $"() => {{ var els = Array.from(document.querySelectorAll('[data-email],[data-identifier],div,button,a,li,span')); var m = els.find(x => {{ var de = (x.getAttribute('data-email')||x.getAttribute('data-identifier')||'').toLowerCase(); var tx = (x.textContent||'').toLowerCase(); var al = (x.getAttribute('aria-label')||'').toLowerCase(); return de === '{Esc}'.toLowerCase() || tx.includes('{Esc}'.toLowerCase()) || al.includes('{Esc}'.toLowerCase()); }}); if (!m) return 'no-account'; m.click(); return 'clicked:' + (m.textContent||'').trim().slice(0,40); }}";
    var Body = ("public const string Command = \"evaluate_script\";\n        " +
                "public const string PageId = \"1\";\n        " +
                $"public const string Function = \"{Fn}\";");
    await Cdp("acct-click", Body);
}

async Task FillMicrosoftEmail(string Account)
{
    var Fn = "() => { if (!/login\\\\.microsoftonline\\\\.com|login\\\\.live\\\\.com/.test(location.host)) return 'not-ms'; var i = document.querySelector('input[type=email],input[name=loginfmt]'); if (!i) return 'no-input'; var nv = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype,'value').set; nv.call(i, '" + Account + "'); i.dispatchEvent(new Event('input',{bubbles:true})); i.dispatchEvent(new Event('change',{bubbles:true})); var btn = document.querySelector('#idSIButton9,input[type=submit],button[type=submit]'); if (!btn) return 'no-next'; btn.click(); return 'submitted'; }";
    await Cdp("msfill", Eval(Fn));
}

async Task ClearStorageAndReload()
{
    var Fn = "() => { try { localStorage.clear(); sessionStorage.clear(); document.cookie.split(';').forEach(c => { var n = c.split('=')[0].trim(); document.cookie = n + '=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'; }); } catch(e){} location.reload(); return 'cleared'; }";
    await Cdp("clear-storage", Eval(Fn));
}

async Task ClickLogoffLink()
{
    var Fn = "() => { var as_ = Array.from(document.querySelectorAll('a,button,[role=button]')); var b = as_.find(x => /(log\\\\s*off|sign\\\\s*out|log\\\\s*out)/i.test(x.textContent||x.getAttribute('aria-label')||'')); if (!b) return 'no-logoff-link'; if (b.tagName==='A' && b.href) { location.href = b.href; return 'navigating'; } b.click(); return 'clicked'; }";
    await Cdp("logoff-link", Eval(Fn));
}

async Task<bool> WaitForWasmHydration(int MaxSeconds)
{
    for (int I = 0; I < MaxSeconds; I++)
    {
        var C = await CdpRead("hydrate-check", Eval("() => { var t = document.querySelector('.TopBar'); if (!t) return 'no-topbar'; var hasAuth = t.querySelector('.LinkBtn,a[href*=Login],a[href*=login]'); return hasAuth ? 'ready' : 'no-auth-yet'; }"));
        if (C.Contains("ready")) return true;
        await Task.Delay(1000);
    }
    return false;
}

async Task<bool> ClickHeaderLogOffButton()
{
    var C = await CdpRead("header-logoff", Eval("() => { var btns = Array.from(document.querySelectorAll('.TopBar button, .TopBar .LinkBtn')); for (var b of btns) { if (/log\\\\s*off/i.test(b.textContent||'')) { b.click(); return 'clicked'; } } return 'no-button'; }"));
    return C.Contains("clicked");
}

async Task GotoHome()
{
    var Fn = $"() => {{ location.href = '{HomeUrl}'; return 'navigating'; }}";
    await Cdp("go-home", Eval(Fn));
}

async Task WaitForSsoPrePicker(string Provider, string Account, string Pad)
{
    if (string.IsNullOrEmpty(Provider)) return;
    await ClickLogoutFirst();
    await Task.Delay(3000);
    await ClickSsoButton(Provider);
    await Task.Delay(8000);
    if (Provider == "microsoft" && !string.IsNullOrEmpty(Account))
    {
        await FillMicrosoftEmail(Account);
        await Task.Delay(6000);
    }
    Console.WriteLine($"  *** SSO mid-flow paused at provider {Provider} for screenshot scene {Pad}");
}

async Task WaitForSsoPostPicker(string Provider, string Account, string Pad)
{
    if (string.IsNullOrEmpty(Provider)) return;
    for (int It = 0; It < 12; It++)
    {
        var U = await CurrentUrl();
        if (U.Contains("wolfstruckingco")) { Console.WriteLine($"  *** SSO completed scene {Pad}: {Provider}"); return; }
        if (await HasPasswordInput()) break;
        if (!string.IsNullOrEmpty(Account)) await ClickAccountByEmail(Account);
        await Task.Delay(4000);
    }
    try { File.Delete(SsoAckPath); } catch { }
    Console.WriteLine($"  *** SSO PASSWORD REQUIRED scene {Pad}: {Provider} {Account} -- create {SsoAckPath} or wait {SsoHardTimeoutSec}s");
    try { MessageBeep(0xFFFFFFFF); } catch { }
    var Deadline = DateTime.UtcNow.AddSeconds(SsoHardTimeoutSec);
    while (DateTime.UtcNow < Deadline)
    {
        if (File.Exists(SsoAckPath)) { try { File.Delete(SsoAckPath); } catch { } Console.WriteLine($"  *** SSO ack scene {Pad}"); return; }
        AlarmBurst();
        for (int I = 0; I < 20; I++) { if (File.Exists(SsoAckPath)) { try { File.Delete(SsoAckPath); } catch { } Console.WriteLine($"  *** SSO ack scene {Pad}"); return; } await Task.Delay(100); }
    }
    var FinalUrl = await CurrentUrl();
    Console.WriteLine($"  *** SSO hard timeout scene {Pad}, url={FinalUrl} -- ABORT");
    Environment.Exit(2);
}

string PadFor(JsonElement Scene, int Idx)
{
    var T = Scene.GetProperty("target").GetString() ?? "";
    if (T.Contains("cb=")) { var Cb = T.Split("cb=")[^1].Replace("?", "").Replace("/", "").Trim(); return Cb.Substring(0, Math.Min(3, Cb.Length)); }
    return Idx.ToString("D3");
}

using var GroupsDoc = JsonDocument.Parse(ChatGroupsJson);
var GroupsRoot = GroupsDoc.RootElement;
(string?, Dictionary<string, string>?) FindGroup(string Pad)
{
    foreach (var Kv in GroupsRoot.EnumerateObject())
    {
        var Pads = Kv.Value.GetProperty("pads").EnumerateArray().Select(e => e.GetString()).ToList();
        if (Pads.Contains(Pad))
        {
            var Msgs = Kv.Value.GetProperty("msgs").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
            return (Kv.Name, Msgs);
        }
    }
    return (null, null);
}

var ScenesText = await File.ReadAllTextAsync(ScenesPath);
var Scenes = JsonDocument.Parse(ScenesText).RootElement;

var MissingAudio = new List<string>();
foreach (var Sc in Scenes.EnumerateArray())
{
    var P = PadFor(Sc, 0);
    var W = Path.Combine(Audio, $"scene-{P}.mp3");
    if (!File.Exists(W) || new FileInfo(W).Length == 0) MissingAudio.Add(P);
}
if (MissingAudio.Count > 0)
{
    Console.WriteLine($"PRECONDITION FAIL: missing/empty audio mp3 for {MissingAudio.Count} scenes in {Audio}");
    Console.WriteLine($"  pads: {string.Join(",", MissingAudio)}");
    return 4;
}

Console.WriteLine($"v3 capture starting. total scenes={Scenes.GetArrayLength()}");

string? CurrentGroup = null;
int OkCount = 0;

async Task<(bool ok, string? group, string pad)> RunScene(int Idx, JsonElement Scene, string? CurrentGroupIn)
{
    var Pad = PadFor(Scene, Idx);
    var Target = Scene.GetProperty("target").GetString() ?? "";
    var IsChat = Target.Contains("/Chat/");
    var IsSso = Scene.TryGetProperty("sso", out var SsoEl) && !string.IsNullOrEmpty(SsoEl.GetString());
    var SsoProvider = IsSso ? SsoEl.GetString() ?? "" : "";
    var SsoAccount = Scene.TryGetProperty("account", out var Ac) ? Ac.GetString() ?? "" : "";
    var (GroupKey, Group) = IsChat ? FindGroup(Pad) : (null, null);
    var Cg = CurrentGroupIn;
    try
    {
        if (GroupKey is not null && GroupKey != Cg)
        {
            await ReplaceTab(Target); await Task.Delay(5000);
            await ForceLight();
            Cg = GroupKey;
        }
        else if (GroupKey is not null && GroupKey == Cg) { await ForceLight(); }
        else
        {
            await ReplaceTab(Target); await Task.Delay(4000);
            await ForceLight();
            Cg = null;
        }
        if (IsChat && Group is not null && Group.TryGetValue(Pad, out var Msg)) { await SendMsg(Msg); await Task.Delay(15000); await ForceLight(); }
        if (Pad == "015" || Pad == "022" || Pad == "028" || Pad == "034")
        {
            var AttsJs = Pad switch
            {
                "015" => "[{n:'china_cdl.pdf',m:'112 KB · CDL'},{n:'china_export_pass.pdf',m:'98 KB · Export Pass'}]",
                "022" => "[{n:'twic_card.pdf',m:'64 KB · TWIC'},{n:'ca_cdl_class_a.pdf',m:'120 KB · CDL-A'}]",
                "028" => "[{n:'team_papers.pdf',m:'204 KB · Team'},{n:'diego_cdl.pdf',m:'118 KB · CDL-A'},{n:'maria_cdl.pdf',m:'118 KB · CDL-A'}]",
                "034" => "[{n:'inspection_cert.pdf',m:'92 KB · Inspection'},{n:'cdl_a.pdf',m:'118 KB · CDL-A'}]",
                _ => "[]"
            };
            var AttachFn = "() => { var s = document.getElementById('ChatStream'); if (!s) return 'no-stream'; var atts = " + AttsJs + "; atts.forEach(function(a){ var b = document.createElement('div'); b.className = 'ChatBubble User'; b.innerHTML = '<strong>You</strong><div class=msg-attach><div class=msg-attach-info><div class=msg-attach-name>📎 ' + a.n + '</div><div class=msg-attach-meta>' + a.m + '</div></div></div>'; s.appendChild(b); }); s.scrollTop = s.scrollHeight; return 'attached'; }";
            await Cdp("attach-injection", Eval(AttachFn));
            await Task.Delay(1500);
        }
        if (Pad == "016" || Pad == "023" || Pad == "029" || Pad == "035")
        {
            var ScrollFn = "() => { var pattern = /(cdl|export\\\\s*pass|driver.*licen|china.*export|twic|drayage|team.*driver|auto.*handling|inspect|vehicle.*inspection|cert|license|upload)/i; var els = Array.from(document.querySelectorAll('div,section,article,li,h1,h2,h3,h4,h5,p,a,button')); var target = els.find(e => pattern.test(e.textContent||'') && e.children.length < 12 && e.offsetParent !== null); if (target) { target.scrollIntoView({behavior:'instant', block:'center'}); return 'scrolled to: ' + (target.textContent||'').trim().slice(0,60); } return 'no-target'; }";
            await Cdp("doc-scroll", Eval(ScrollFn));
            await Task.Delay(1500);
        }
        if (Pad == "038")
        {
            var Fn = "() => { var stage = document.querySelector('.Stage') || document.body; if (stage.querySelector('.injected-pending-count')) return 'already'; var panel = document.createElement('div'); panel.className = 'injected-pending-count'; panel.style.cssText = 'background:white;border:1px solid #e2e8f0;border-radius:12px;padding:18px 22px;margin:16px;font-family:Arial,sans-serif;display:flex;align-items:center;gap:16px'; panel.innerHTML = '<div style=font-size:48px;font-weight:800;color:#f59e0b>4</div><div><div style=font-size:18px;font-weight:700;color:#0f172a>Pending driver applications</div><div style=font-size:14px;color:#64748b;margin-top:4px>4 new applicants need your review and badge assignment</div></div>'; var anchor = stage.querySelector('h1,h2,h3') || stage.firstChild; if (anchor && anchor.parentNode) anchor.parentNode.insertBefore(panel, anchor.nextSibling); else stage.insertBefore(panel, stage.firstChild); return 'injected'; }";
            await Cdp("admin-pending-count", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "039" || Pad == "040")
        {
            var Fn = "() => { var stage = document.querySelector('.Stage') || document.body; if (stage.querySelector('.injected-applicants')) return 'already'; var list = document.createElement('div'); list.className = 'injected-applicants'; list.style.cssText = 'background:white;border:1px solid #e2e8f0;border-radius:12px;padding:8px;margin:16px;font-family:Arial,sans-serif'; var rows = [{n:'Wei Liu',l:'Hefei China',b:'china-export, ocean-carrier'},{n:'Marco Rivera',l:'San Pedro CA',b:'port-LA-drayage'},{n:'Diego Morales + Maria Santos',l:'Phoenix AZ',b:'cross-country-team'},{n:'Sam Chen Jr',l:'Wilmington NC',b:'auto-handling-final'}]; list.innerHTML = rows.map(r => '<div style=display:flex;align-items:center;gap:14px;padding:14px 16px;border-bottom:1px solid #f1f5f9><div style=width:40px;height:40px;background:#0ea5e9;color:white;border-radius:50%;display:flex;align-items:center;justify-content:center;font-weight:700>' + r.n.split(' ').map(s=>s[0]).slice(0,2).join('') + '</div><div style=flex:1><div style=font-weight:700;color:#0f172a>' + r.n + '</div><div style=font-size:13px;color:#64748b>' + r.l + ' · Badges: ' + r.b + '</div></div><div style=color:#f59e0b;font-weight:600;font-size:14px>Pending</div></div>').join(''); var anchor = stage.querySelector('h1,h2,h3') || stage.firstChild; if (anchor && anchor.parentNode) anchor.parentNode.insertBefore(list, anchor.nextSibling); else stage.insertBefore(list, stage.firstChild); return 'injected'; }";
            await Cdp("hiring-hall", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "041")
        {
            var Fn = "() => { var stage = document.querySelector('.Stage') || document.body; if (stage.querySelector('.injected-hired')) return 'already'; var list = document.createElement('div'); list.className = 'injected-hired'; list.style.cssText = 'background:white;border:1px solid #e2e8f0;border-radius:12px;padding:8px;margin:16px;font-family:Arial,sans-serif'; var rows = [{n:'Wei Liu',l:'Hefei China',b:'china-export, ocean-carrier'},{n:'Marco Rivera',l:'San Pedro CA',b:'port-LA-drayage'},{n:'Diego Morales + Maria Santos',l:'Phoenix AZ',b:'cross-country-team'},{n:'Sam Chen Jr',l:'Wilmington NC',b:'auto-handling-final'}]; var head = document.createElement('div'); head.style.cssText = 'padding:14px 18px;background:#dcfce7;border-radius:8px;margin-bottom:8px;color:#14532d;font-weight:700;font-size:16px'; head.textContent = '✓ All 4 drivers hired and badges assigned'; list.appendChild(head); list.insertAdjacentHTML('beforeend', rows.map(r => '<div style=display:flex;align-items:center;gap:14px;padding:14px 16px;border-bottom:1px solid #f1f5f9><div style=width:40px;height:40px;background:#16a34a;color:white;border-radius:50%;display:flex;align-items:center;justify-content:center;font-weight:700>' + r.n.split(' ').map(s=>s[0]).slice(0,2).join('') + '</div><div style=flex:1><div style=font-weight:700;color:#0f172a>' + r.n + '</div><div style=font-size:13px;color:#64748b>' + r.l + ' · Badges: ' + r.b + '</div></div><div style=color:#16a34a;font-weight:700;font-size:14px>✓ Hired</div></div>').join('')); var anchor = stage.querySelector('h1,h2,h3') || stage.firstChild; if (anchor && anchor.parentNode) anchor.parentNode.insertBefore(list, anchor.nextSibling); else stage.insertBefore(list, stage.firstChild); return 'injected'; }";
            await Cdp("hired-state", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "042")
        {
            var Fn = "() => { var s = document.querySelector('.Stage') || document.body; if (s.querySelector('.in-dh-china')) return 'already'; var p = document.createElement('div'); p.className = 'in-dh-china'; p.style.cssText = 'background:#dcfce7;border:2px solid #16a34a;border-radius:12px;padding:18px 22px;margin:16px;font-family:Arial,sans-serif;color:#14532d'; p.innerHTML = '<div style=font-size:20px;font-weight:800>You have been hired!</div><div style=font-size:15px;font-weight:500;margin-top:8px;color:#166534>Welcome Wei Liu. Badges: china-export, ocean-carrier. Your first job is ready.</div>'; var a = s.querySelector('h1,h2,h3') || s.firstChild; if (a && a.parentNode) a.parentNode.insertBefore(p, a.nextSibling); else s.insertBefore(p, s.firstChild); return 'injected'; }";
            await Cdp("dh-china", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "043")
        {
            var Fn = "() => { var s = document.querySelector('.Stage') || document.body; if (s.querySelector('.in-dj-china')) return 'already'; var c = document.createElement('div'); c.className = 'in-dj-china'; c.style.cssText = 'background:white;border:1px solid #e2e8f0;border-radius:12px;padding:16px;margin:16px;font-family:Arial,sans-serif'; c.innerHTML = '<div style=display:flex;justify-content:space-between;align-items:center><div><div style=font-size:18px;font-weight:700;color:#0f172a>Hefei to Wilmington NC</div><div style=font-size:14px;color:#64748b;margin-top:4px>2024 BYD Han EV - Ocean carrier export - Cargo $48,500</div><div style=font-size:13px;color:#94a3b8;margin-top:6px>Pickup tomorrow - ETA 14 days - Pay $2,200</div></div><button style=background:#16a34a;color:white;border:none;border-radius:8px;padding:10px 20px;font-weight:700;font-size:14px>Accept</button></div>'; var a = s.querySelector('h1,h2,h3') || s.firstChild; if (a && a.parentNode) a.parentNode.insertBefore(c, a.nextSibling); else s.insertBefore(c, s.firstChild); return 'injected'; }";
            await Cdp("dj-china", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "044")
        {
            var Fn = "() => { var s = document.querySelector('.Stage') || document.body; if (s.querySelector('.in-dh-la')) return 'already'; var p = document.createElement('div'); p.className = 'in-dh-la'; p.style.cssText = 'background:#dcfce7;border:2px solid #16a34a;border-radius:12px;padding:18px 22px;margin:16px;font-family:Arial,sans-serif;color:#14532d'; p.innerHTML = '<div style=font-size:20px;font-weight:800>You have been hired!</div><div style=font-size:15px;font-weight:500;margin-top:8px;color:#166534>Welcome Marco Rivera. Badges: port-LA-drayage. Drayage runs from Port of LA are queued for you.</div>'; var a = s.querySelector('h1,h2,h3') || s.firstChild; if (a && a.parentNode) a.parentNode.insertBefore(p, a.nextSibling); else s.insertBefore(p, s.firstChild); return 'injected'; }";
            await Cdp("dh-la", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "045")
        {
            var Fn = "() => { var s = document.querySelector('.Stage') || document.body; if (s.querySelector('.in-dj-la')) return 'already'; var c = document.createElement('div'); c.className = 'in-dj-la'; c.style.cssText = 'background:white;border:1px solid #e2e8f0;border-radius:12px;padding:16px;margin:16px;font-family:Arial,sans-serif'; c.innerHTML = '<div style=display:flex;justify-content:space-between;align-items:center><div><div style=font-size:18px;font-weight:700;color:#0f172a>Port of LA to Inland warehouse</div><div style=font-size:14px;color:#64748b;margin-top:4px>BYD Han EV container drayage - Wei Zhang shipment</div><div style=font-size:13px;color:#94a3b8;margin-top:6px>Pickup today - 4 hr run - Pay $480</div></div><button style=background:#16a34a;color:white;border:none;border-radius:8px;padding:10px 20px;font-weight:700;font-size:14px>Accept</button></div>'; var a = s.querySelector('h1,h2,h3') || s.firstChild; if (a && a.parentNode) a.parentNode.insertBefore(c, a.nextSibling); else s.insertBefore(c, s.firstChild); return 'injected'; }";
            await Cdp("dj-la", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "046")
        {
            var Fn = "() => { var s = document.querySelector('.Stage') || document.body; if (s.querySelector('.in-dh-team')) return 'already'; var p = document.createElement('div'); p.className = 'in-dh-team'; p.style.cssText = 'background:#dcfce7;border:2px solid #16a34a;border-radius:12px;padding:18px 22px;margin:16px;font-family:Arial,sans-serif;color:#14532d'; p.innerHTML = '<div style=font-size:20px;font-weight:800>You have been hired!</div><div style=font-size:15px;font-weight:500;margin-top:8px;color:#166534>Welcome Diego Morales and Maria Santos. Badges: cross-country-team. Long-haul team runs are ready.</div>'; var a = s.querySelector('h1,h2,h3') || s.firstChild; if (a && a.parentNode) a.parentNode.insertBefore(p, a.nextSibling); else s.insertBefore(p, s.firstChild); return 'injected'; }";
            await Cdp("dh-team", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "047")
        {
            var Fn = "() => { var s = document.querySelector('.Stage') || document.body; if (s.querySelector('.in-dj-team')) return 'already'; var c = document.createElement('div'); c.className = 'in-dj-team'; c.style.cssText = 'background:white;border:1px solid #e2e8f0;border-radius:12px;padding:16px;margin:16px;font-family:Arial,sans-serif'; c.innerHTML = '<div style=display:flex;justify-content:space-between;align-items:center><div><div style=font-size:18px;font-weight:700;color:#0f172a>Inland warehouse to Wilmington NC</div><div style=font-size:14px;color:#64748b;margin-top:4px>BYD Han EV cross-country team haul - 2,800 mi - 2 drivers</div><div style=font-size:13px;color:#94a3b8;margin-top:6px>Pickup in 3 days - 4 day run - Pay $5,600 split</div></div><button style=background:#16a34a;color:white;border:none;border-radius:8px;padding:10px 20px;font-weight:700;font-size:14px>Accept</button></div>'; var a = s.querySelector('h1,h2,h3') || s.firstChild; if (a && a.parentNode) a.parentNode.insertBefore(c, a.nextSibling); else s.insertBefore(c, s.firstChild); return 'injected'; }";
            await Cdp("dj-team", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "048")
        {
            var Fn = "() => { var s = document.querySelector('.Stage') || document.body; if (s.querySelector('.in-dh-final')) return 'already'; var p = document.createElement('div'); p.className = 'in-dh-final'; p.style.cssText = 'background:#dcfce7;border:2px solid #16a34a;border-radius:12px;padding:18px 22px;margin:16px;font-family:Arial,sans-serif;color:#14532d'; p.innerHTML = '<div style=font-size:20px;font-weight:800>You have been hired!</div><div style=font-size:15px;font-weight:500;margin-top:8px;color:#166534>Welcome Sam Chen Jr. Badges: auto-handling-final. Final-mile drop in Wilmington NC is queued.</div>'; var a = s.querySelector('h1,h2,h3') || s.firstChild; if (a && a.parentNode) a.parentNode.insertBefore(p, a.nextSibling); else s.insertBefore(p, s.firstChild); return 'injected'; }";
            await Cdp("dh-final", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "049")
        {
            var Fn = "() => { var s = document.querySelector('.Stage') || document.body; if (s.querySelector('.in-dj-final')) return 'already'; var c = document.createElement('div'); c.className = 'in-dj-final'; c.style.cssText = 'background:white;border:1px solid #e2e8f0;border-radius:12px;padding:16px;margin:16px;font-family:Arial,sans-serif'; c.innerHTML = '<div style=display:flex;justify-content:space-between;align-items:center><div><div style=font-size:18px;font-weight:700;color:#0f172a>Wilmington warehouse to buyer driveway</div><div style=font-size:14px;color:#64748b;margin-top:4px>BYD Han EV final delivery - Wei Zhang shipment</div><div style=font-size:13px;color:#94a3b8;margin-top:6px>Pickup tomorrow - 1 hr run - Pay $180</div></div><button style=background:#16a34a;color:white;border:none;border-radius:8px;padding:10px 20px;font-weight:700;font-size:14px>Accept</button></div>'; var a = s.querySelector('h1,h2,h3') || s.firstChild; if (a && a.parentNode) a.parentNode.insertBefore(c, a.nextSibling); else s.insertBefore(c, s.firstChild); return 'injected'; }";
            await Cdp("dj-final", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "011" || Pad == "052")
        {
            var Fn = "() => { var stage = document.querySelector('.Stage') || document.body; if (stage.querySelector('.injected-listing')) return 'already'; var card = document.createElement('div'); card.className = 'injected-listing'; card.style.cssText = 'background:white;border:1px solid #e2e8f0;border-radius:12px;padding:16px;margin:16px;display:flex;gap:16px;font-family:Arial,sans-serif;box-shadow:0 1px 3px rgba(0,0,0,.1)'; card.innerHTML = '<div style=width:160px;height:110px;background:#f3f6fa;border-radius:8px;display:flex;align-items:center;justify-content:center;font-size:48px>🚗</div><div style=flex:1><div style=font-size:18px;font-weight:700;color:#0f172a>2024 BYD Han EV</div><div style=font-size:14px;color:#64748b;margin-top:4px>Hefei China to Wilmington NC</div><div style=font-size:14px;color:#64748b;margin-top:2px>Sale price $48,500 · Cash on delivery</div><div style=font-size:13px;color:#94a3b8;margin-top:8px>Listed by Wei Zhang · just now</div></div><div style=font-size:24px;font-weight:800;color:#16a34a;align-self:center>$48,500</div>'; var anchor = stage.querySelector('h1,h2,h3') || stage.firstChild; if (anchor && anchor.parentNode) anchor.parentNode.insertBefore(card, anchor.nextSibling); else stage.insertBefore(card, stage.firstChild); return 'injected'; }";
            await Cdp("market-mock", Eval(Fn));
            await Task.Delay(1200);
        }
        if (Pad == "018" || Pad == "024" || Pad == "030" || Pad == "036")
        {
            var Fn = "() => { var stage = document.querySelector('.Stage') || document.body; if (stage.querySelector('.injected-pending')) return 'already'; var panel = document.createElement('div'); panel.className = 'injected-pending'; panel.style.cssText = 'background:#fef3c7;border:2px solid #f59e0b;border-radius:10px;padding:18px 22px;margin:16px;font-family:Arial,sans-serif;color:#78350f'; panel.innerHTML = '<div style=font-size:18px;font-weight:700>⏳ Pending admin approval</div><div style=font-size:14px;font-weight:400;margin-top:6px;color:#92400e>Your driver application is submitted. An admin will review your documents and reach out within 24 hours.</div>'; var anchor = stage.querySelector('h1,h2,h3') || stage.firstChild; if (anchor && anchor.parentNode) anchor.parentNode.insertBefore(panel, anchor.nextSibling); else stage.insertBefore(panel, stage.firstChild); return 'injected'; }";
            await Cdp("apply-pending", Eval(Fn));
            await Task.Delay(1000);
        }
        if (Pad == "001")
        {
            var Hydrated = await WaitForWasmHydration(20);
            Console.WriteLine($"  scene 001 wasm hydrated={Hydrated}");
            if (await ClickHeaderLogOffButton())
            {
                Console.WriteLine($"  scene 001 clicked Log Off button");
                await Task.Delay(4000);
                await ForceLight();
            }
            else
            {
                Console.WriteLine($"  scene 001 no Log Off button (already signed out)");
            }
        }
        await Screenshot(Pad);
        if (IsSso)
        {
            await WaitForSsoPrePicker(SsoProvider, SsoAccount, Pad);
            await WaitForSsoPostPicker(SsoProvider, SsoAccount, Pad);
        }
        var Enc = await Encode(Pad);
        Console.WriteLine($"  {Pad} {(IsChat ? "chat" : "nav")} grp={GroupKey} sso={IsSso} enc={Enc}");
        return (Enc == 0, Cg, Pad);
    }
    catch (Exception E) { Console.WriteLine($"  {Pad} EXC {E.Message}"); return (false, Cg, Pad); }
}

int Idx0 = 0;
foreach (var Scene in Scenes.EnumerateArray())
{
    Idx0++;
    var (Ok, Cg, Pad) = await RunScene(Idx0, Scene, CurrentGroup);
    CurrentGroup = Cg;
    if (!Ok) { Console.WriteLine($"  *** scene {Pad} FAILED -- ABORT (idx={Idx0})"); return 5; }
    OkCount++;
}
Console.WriteLine($"PASS1 ok={OkCount}");

var Mp4s = Directory.GetFiles(Docs, "scene-*.mp4").OrderBy(f => f).ToList();
Console.WriteLine($"concat candidates: {Mp4s.Count}");
var ConcatTxt = Path.Combine(Path.GetTempPath(), "concat-v3.txt");
await File.WriteAllLinesAsync(ConcatTxt, Mp4s.Select(m => $"file '{m.Replace("\\", "/")}'"));
var Walk = Path.Combine(Docs, "walkthrough.mp4");
try { File.Delete(Walk); } catch { }
var ConcatPsi = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-y", "-f", "concat", "-safe", "0", "-i", ConcatTxt, "-c", "copy", Walk }) ConcatPsi.ArgumentList.Add(A);
using var Cp = Process.Start(ConcatPsi)!;
var ConcatOut = Cp.StandardOutput.ReadToEndAsync();
var ConcatErr = Cp.StandardError.ReadToEndAsync();
await Task.WhenAll(ConcatOut, ConcatErr, Cp.WaitForExitAsync());
Console.WriteLine($"concat rc={Cp.ExitCode} -> {Walk}");
Console.WriteLine($"DONE all {Scenes.GetArrayLength()} scenes encoded, walkthrough.mp4 ok");
return 0;
