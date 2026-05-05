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

var ServeHttp = new System.Net.Http.HttpClient();
ServeHttp.Timeout = TimeSpan.FromSeconds(60);
Process? ServeProc = null;
async Task EnsureServeAsync()
{
    if (ServeProc != null && !ServeProc.HasExited) return;
    var Cfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        public const string Command = \"serve\";\n    }\n}\n";
    var Tmp = Path.Combine(Path.GetTempPath(), $"serve-{Guid.NewGuid():N}.cs");
    await File.WriteAllTextAsync(Tmp, Cfg);
    var P = new ProcessStartInfo("dotnet") { WorkingDirectory = Repo, UseShellExecute = false };
    P.ArgumentList.Add("run"); P.ArgumentList.Add(Path.Combine(Repo, "scripts", "generic", "chrome-devtools.cs")); P.ArgumentList.Add(Tmp);
    ServeProc = Process.Start(P)!;
    await Task.Delay(15000);
}
async Task<string> PostServeAsync(string Ln)
{
    try { using var Resp = await ServeHttp.PostAsync("http://127.0.0.1:9333/", new System.Net.Http.StringContent(Ln)); return await Resp.Content.ReadAsStringAsync(); } catch { return ""; }
}
string BodyToLine(string Body)
{
    var Args = new List<string>();
    string? Cmd = null;
    foreach (var Ln in Body.Split('\n'))
    {
        var Tr = Ln.Trim();
        if (!Tr.StartsWith("public const string ")) continue;
        var Rest = Tr.Substring(20);
        var Eq = Rest.IndexOf(" = ");
        if (Eq < 0) continue;
        var K = Rest.Substring(0, Eq);
        var V = Rest.Substring(Eq + 3);
        if (V.StartsWith("@")) V = V.Substring(1);
        if (V.StartsWith("\"")) V = V.Substring(1);
        if (V.EndsWith(";")) V = V.Substring(0, V.Length - 1);
        if (V.EndsWith("\"")) V = V.Substring(0, V.Length - 1);
        if (K == "Command") Cmd = V;
        else if (K == "OutputPath") { }
        else Args.Add("--" + char.ToLowerInvariant(K[0]) + K.Substring(1) + " \"" + V + "\"");
    }
    return Cmd + (Args.Count > 0 ? " " + string.Join(" ", Args) : "");
}

string CachedWolfsPageIdx = "";
async Task<int> Cdp(string Name, string Body)
{
    await EnsureServeAsync();
    if (Name != "list" && Body.Contains("PageId = \"1\""))
    {
        if (string.IsNullOrEmpty(CachedWolfsPageIdx))
        {
            var Listing = await PostServeAsync("list_pages");
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
    var Line2 = BodyToLine(Body);
    await PostServeAsync(Line2);
    await Task.Delay(300);
    return 0;
}

async Task<string> CdpRead(string Name, string Body)
{
    await EnsureServeAsync();
    if (Name != "list" && Body.Contains("PageId = \"1\""))
    {
        if (string.IsNullOrEmpty(CachedWolfsPageIdx))
        {
            var Listing = await PostServeAsync("list_pages");
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
    var Line3 = BodyToLine(Body);
    return await PostServeAsync(Line3);
}



async Task NewPageAt(string Url) => await Cdp("new", $"public const string Command = \"new_page\";\n        public const string Url = \"{Url.Replace("\"", "\\\"")}\";");

async Task ReplaceTab(string Url) => await NewPageAt(Url);

async Task ForceLight() => await Task.CompletedTask;

async Task SendMsg(string Msg)
{
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    var Snap = await CdpRead("snap-input", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    var InputRx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+textbox", System.Text.RegularExpressions.RegexOptions.Multiline);
    var InputMatch = InputRx.Match(Snap);
    if (!InputMatch.Success) return;
    var EscMsg = Msg.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    await Cdp("fill-msg", "public const string Command = \"fill\";\n        public const string PageId = \"" + Pid + "\";\n        public const string Uid = \"" + InputMatch.Groups[1].Value + "\";\n        public const string Value = \"" + EscMsg + "\";");
    var Snap2 = await CdpRead("snap-send", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    var SendRx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+button\s+""[^""]*Send", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    var SendMatch = SendRx.Match(Snap2);
    if (!SendMatch.Success) return;
    await Cdp("click-send", "public const string Command = \"click\";\n        public const string PageId = \"" + Pid + "\";\n        public const string Uid = \"" + SendMatch.Groups[1].Value + "\";");
}

async Task<int> Screenshot(string Pad)
{
    var Out = Path.Combine(Frames, Pad + ".png");
    try { File.Delete(Out); } catch { }
    var PageIndices = new List<string>();
    if (!string.IsNullOrEmpty(CachedWolfsPageIdx)) PageIndices.Add(CachedWolfsPageIdx);
    foreach (var I in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" }) if (!PageIndices.Contains(I)) PageIndices.Add(I);
    foreach (var PageIdx in PageIndices)
    {
        var Body = $"public const string Command = \"take_screenshot\";\n        public const string PageId = \"{PageIdx}\";\n        public const string FilePath = @\"{Out}\";";
        var Rc = await Cdp("shot", Body);
        await Task.Delay(800);
        if (File.Exists(Out) && new FileInfo(Out).Length > 0)
        {
            Console.WriteLine($"  shot pad={Pad} via pageIdx={PageIdx}");
            return Rc;
        }
        try { File.Delete(Out); } catch { }
    }
    Console.WriteLine($"  Screenshot FAIL pad={Pad} png-missing on all PageIdx 1-12");
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
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    var Snap = await CdpRead("snap-pw", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    return Snap.Contains("Password", StringComparison.OrdinalIgnoreCase);
}

async Task<string> CurrentUrl()
{
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    var Snap = await CdpRead("snap-url", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    if (Snap.Contains("Wolfs Trucking", StringComparison.OrdinalIgnoreCase) || Snap.Contains("Sign In", StringComparison.OrdinalIgnoreCase)) return "wolfstruckingco";
    return "external";
}

async Task ClickLogoutFirst()
{
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    var Snap = await CdpRead("snap-logout", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    var Rx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+(?:button|link)\s+""[^""]*(?:Log\s*Out|Sign\s*Out|Log\s*Off)", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    var M = Rx.Match(Snap);
    if (!M.Success) return;
    await Cdp("click-logout", "public const string Command = \"click\";\n        public const string PageId = \"" + Pid + "\";\n        public const string Uid = \"" + M.Groups[1].Value + "\";");
}

async Task ClickSsoButton(string Provider)
{
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    var Snap = await CdpRead("snap-sso", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    var Rx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+(?:button|link)\s+""[^""]*" + System.Text.RegularExpressions.Regex.Escape(Provider), System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    var M = Rx.Match(Snap);
    if (!M.Success) return;
    await Cdp("click-sso", "public const string Command = \"click\";\n        public const string PageId = \"" + Pid + "\";\n        public const string Uid = \"" + M.Groups[1].Value + "\";");
}

async Task ClickAccountByEmail(string Email)
{
    if (string.IsNullOrEmpty(Email)) return;
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    var Snap = await CdpRead("snap-account", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    var Rx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+(?:button|link|listitem|option)\s+""[^""]*" + System.Text.RegularExpressions.Regex.Escape(Email), System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    var M = Rx.Match(Snap);
    if (!M.Success) return;
    await Cdp("click-account", "public const string Command = \"click\";\n        public const string PageId = \"" + Pid + "\";\n        public const string Uid = \"" + M.Groups[1].Value + "\";");
}

async Task FillMicrosoftEmail(string Account)
{
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    var Snap = await CdpRead("snap-msemail", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    var InputRx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+textbox", System.Text.RegularExpressions.RegexOptions.Multiline);
    var InputMatch = InputRx.Match(Snap);
    if (!InputMatch.Success) return;
    var EscAccount = Account.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    await Cdp("fill-msemail", "public const string Command = \"fill\";\n        public const string PageId = \"" + Pid + "\";\n        public const string Uid = \"" + InputMatch.Groups[1].Value + "\";\n        public const string Value = \"" + EscAccount + "\";");
    var Snap2 = await CdpRead("snap-mssubmit", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    var SubmitRx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+button\s+""[^""]*(?:Next|Sign in|submit|Continue)", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    var SubmitMatch = SubmitRx.Match(Snap2);
    if (!SubmitMatch.Success) return;
    await Cdp("click-mssubmit", "public const string Command = \"click\";\n        public const string PageId = \"" + Pid + "\";\n        public const string Uid = \"" + SubmitMatch.Groups[1].Value + "\";");
}

async Task FillMicrosoftEmailNoSubmit(string Account)
{
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    var Snap = await CdpRead("snap-msnosubmit", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    var InputRx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+textbox", System.Text.RegularExpressions.RegexOptions.Multiline);
    var InputMatch = InputRx.Match(Snap);
    if (!InputMatch.Success) return;
    var EscAccount = Account.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    await Cdp("fill-msnosubmit", "public const string Command = \"fill\";\n        public const string PageId = \"" + Pid + "\";\n        public const string Uid = \"" + InputMatch.Groups[1].Value + "\";\n        public const string Value = \"" + EscAccount + "\";");
}

async Task<bool> WaitForWasmHydration(int MaxSeconds)
{
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    for (int I = 0; I < MaxSeconds; I++)
    {
        var Snap = await CdpRead("snap-hydrate", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
        if (Snap.Contains("Sign In", StringComparison.OrdinalIgnoreCase) || Snap.Contains("Log Off", StringComparison.OrdinalIgnoreCase)) return true;
        await Task.Delay(1000);
    }
    return false;
}

async Task<bool> ClickHeaderLogOffButton()
{
    var Pid = string.IsNullOrEmpty(CachedWolfsPageIdx) ? "1" : CachedWolfsPageIdx;
    var Snap = await CdpRead("snap-logoff", "public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + Pid + "\";");
    var Rx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+(?:button|link)\s+""[^""]*Log\s*Off", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    var M = Rx.Match(Snap);
    if (!M.Success) return false;
    await Cdp("click-logoff", "public const string Command = \"click\";\n        public const string PageId = \"" + Pid + "\";\n        public const string Uid = \"" + M.Groups[1].Value + "\";");
    return true;
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
    bool FoundPassword = false;
    for (int It = 0; It < 12; It++)
    {
        var U = await CurrentUrl();
        if (U.Contains("wolfstruckingco")) { Console.WriteLine($"  *** SSO completed scene {Pad}: {Provider}"); return; }
        if (await HasPasswordInput()) { FoundPassword = true; break; }
        if (!string.IsNullOrEmpty(Account)) await ClickAccountByEmail(Account);
        await Task.Delay(4000);
    }
    if (!FoundPassword) { Console.WriteLine($"  SSO scene {Pad} no password input visible after {12*4}s; continuing without alarm"); return; }
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

Console.WriteLine($"v4 (serve-mode) capture starting. total scenes={Scenes.GetArrayLength()}");

string? CurrentGroup = null;
int OkCount = 0;

async Task<(bool ok, string? group, string pad)> RunScene(int Idx, JsonElement Scene, string? CurrentGroupIn)
{
    var Pad = PadFor(Scene, Idx);
    var Target = Scene.GetProperty("target").GetString() ?? "";
    if (!Target.Contains("theme=", StringComparison.Ordinal))
    {
        Target = Target + (Target.Contains('?') ? "&theme=light" : "?theme=light");
    }
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
        if (Pad == "051")
        {
            await ClickLogoutFirst();
            await Task.Delay(3000);
            await ClickSsoButton("microsoft");
            await Task.Delay(8000);
            await FillMicrosoftEmailNoSubmit(SsoAccount);
            await Task.Delay(2000);
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
    if (!Ok) { Console.WriteLine($"  *** scene {Pad} FAILED -- ABORT (idx={Idx0})"); try { ServeProc?.Kill(true); } catch { } return 5; }
    OkCount++;
}
Console.WriteLine($"PASS1 ok={OkCount}");

var Mp4s = Directory.GetFiles(Docs, "scene-*.mp4").OrderBy(f => f).ToList();
Console.WriteLine($"concat candidates: {Mp4s.Count}");
var ConcatTxt = Path.Combine(Path.GetTempPath(), "concat-v4.txt");
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
try { ServeProc?.Kill(true); } catch { }
return 0;
