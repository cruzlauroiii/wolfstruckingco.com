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

async Task<int> Cdp(string Name, string Body)
{
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

async Task<int> Screenshot(string Pad)
{
    var Out = Path.Combine(Frames, Pad + ".png");
    try { File.Delete(Out); } catch { }
    var Body = $"public const string Command = \"take_screenshot\";\n        public const string PageId = \"1\";\n        public const string FilePath = @\"{Out}\";";
    var Rc = await Cdp("shot", Body);
    if (!File.Exists(Out) || new FileInfo(Out).Length == 0) { Console.WriteLine($"  Screenshot FAIL pad={Pad} png-missing-or-empty rc={Rc}"); return -3; }
    return Rc;
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

async Task ProviderLogout(string Provider)
{
    var Url = Provider switch
    {
        "google" => "https://accounts.google.com/Logout",
        "microsoft" => "https://login.microsoftonline.com/common/oauth2/v2.0/logout",
        "github" => "https://github.com/logout",
        _ => null,
    };
    if (Url is null) return;
    var Esc = Url.Replace("\"", "\\\"");
    await Cdp("provider-logout", $"public const string Command = \"new_page\";\n        public const string Url = \"{Esc}\";");
    await Task.Delay(5000);
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

async Task GotoHome()
{
    var Fn = $"() => {{ location.href = '{HomeUrl}'; return 'navigating'; }}";
    await Cdp("go-home", Eval(Fn));
}

async Task WaitForSso(string Provider, string Account, string Pad)
{
    if (!string.IsNullOrEmpty(Provider))
    {
        await ProviderLogout(Provider);
        await ClickLogoutFirst();
        await Task.Delay(2000);
        await ClickSsoButton(Provider);
        await Task.Delay(10000);
        if (Provider == "microsoft" && !string.IsNullOrEmpty(Account))
        {
            await FillMicrosoftEmail(Account);
            await Task.Delay(8000);
        }
        for (int It = 0; It < 12; It++)
        {
            var U = await CurrentUrl();
            if (U.Contains("wolfstruckingco")) { Console.WriteLine($"  *** SSO auto scene {Pad}: {Provider}"); return; }
            if (await HasPasswordInput()) break;
            if (!string.IsNullOrEmpty(Account)) await ClickAccountByEmail(Account);
            await Task.Delay(4000);
        }
    }
    try { File.Delete(SsoAckPath); } catch { }
    Console.WriteLine($"  *** SSO PASSWORD REQUIRED scene {Pad}: {Provider} {Account} -- create {SsoAckPath} or wait {SsoHardTimeoutSec}s");
    try { MessageBeep(0xFFFFFFFF); } catch { }
    var Deadline = DateTime.UtcNow.AddSeconds(SsoHardTimeoutSec);
