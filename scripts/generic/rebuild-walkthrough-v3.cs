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
        if (Pad == "015" || Pad == "022" || Pad == "029" || Pad == "036")
        {
            var AttsJs = Pad switch
            {
                "015" => "[{n:'china_cdl.pdf',m:'112 KB · CDL'},{n:'china_export_pass.pdf',m:'98 KB · Export Pass'}]",
                "022" => "[{n:'twic_card.pdf',m:'64 KB · TWIC'},{n:'ca_cdl_class_a.pdf',m:'120 KB · CDL-A'}]",
                "029" => "[{n:'team_papers.pdf',m:'204 KB · Team'},{n:'diego_cdl.pdf',m:'118 KB · CDL-A'},{n:'maria_cdl.pdf',m:'118 KB · CDL-A'}]",
                "036" => "[{n:'auto_handling_cert.pdf',m:'88 KB · Auto Handling'}]",
                _ => "[]"
            };
            var AttachFn = "() => { var s = document.getElementById('ChatStream'); if (!s) return 'no-stream'; var atts = " + AttsJs + "; atts.forEach(function(a){ var b = document.createElement('div'); b.className = 'ChatBubble User'; b.innerHTML = '<strong>You</strong><div class=msg-attach><div class=msg-attach-info><div class=msg-attach-name>📎 ' + a.n + '</div><div class=msg-attach-meta>' + a.m + '</div></div></div>'; s.appendChild(b); }); s.scrollTop = s.scrollHeight; return 'attached'; }";
            await Cdp("attach-injection", Eval(AttachFn));
            await Task.Delay(1500);
        }
        if (Pad == "016")
        {
            var ScrollFn = "() => { var pattern = /(cdl|export\\\\s*pass|driver.*licen|china.*export)/i; var els = Array.from(document.querySelectorAll('div,section,article,li,h1,h2,h3,h4,h5,p,a,button')); var target = els.find(e => pattern.test(e.textContent||'') && e.children.length < 12 && e.offsetParent !== null); if (target) { target.scrollIntoView({behavior:'instant', block:'center'}); return 'scrolled to: ' + (target.textContent||'').trim().slice(0,60); } return 'no-target'; }";
            await Cdp("scene-016-scroll", Eval(ScrollFn));
            await Task.Delay(1500);
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
