#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run run-scene-v2.cs scene-NNN-config.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { Console.Error.WriteLine($"missing: {SpecPath}"); return 2; }
var Spec = await File.ReadAllTextAsync(SpecPath);

string Get(string Name)
{
    var Rx = new Regex("const\\s+string\\s+" + Name + "\\s*=\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\"");
    var M = Rx.Match(Spec);
    return M.Success ? M.Groups[1].Value : "";
}

var Repo = Get("Repo"); if (string.IsNullOrEmpty(Repo)) Repo = @"C:\repo\public\wolfstruckingco.com\main";
var Frames = Get("Frames"); if (string.IsNullOrEmpty(Frames)) Frames = @"C:\Users\user1\AppData\Local\Temp\wolfs-frames";
var Audio = Get("Audio"); if (string.IsNullOrEmpty(Audio)) Audio = @"C:\Users\user1\AppData\Local\Temp\wolfs-video\audio-edge";
var Docs = Get("Docs"); if (string.IsNullOrEmpty(Docs)) Docs = @"C:\repo\public\wolfstruckingco.com\main\docs\videos";
var Pad = Get("Pad");
var Url = Get("Url");
var RequiresLogin = Get("RequiresLogin").ToLowerInvariant() == "true";
var LoginProvider = Get("LoginProvider");
var LoginEmail = Get("LoginEmail");
Directory.CreateDirectory(Frames);

async Task<int> Cdp(string Body)
{
    var Tmp = Path.Combine(Path.GetTempPath(), $"cdp-{Pad}-{Guid.NewGuid():N}.cs");
    var Cfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        " + Body + "\n    }\n}\n";
    await File.WriteAllTextAsync(Tmp, Cfg);
    var P = new ProcessStartInfo("dotnet") { WorkingDirectory = Repo, RedirectStandardOutput = true, RedirectStandardError = true };
    P.ArgumentList.Add("run");
    P.ArgumentList.Add(Path.Combine(Repo, "scripts", "generic", "chrome-devtools.cs"));
    P.ArgumentList.Add(Tmp);
    using var Pp = Process.Start(P)!;
    var Wt = Pp.WaitForExitAsync();
    if (await Task.WhenAny(Wt, Task.Delay(60000)) != Wt) { try { Pp.Kill(true); } catch {} return -1; }
    try { File.Delete(Tmp); } catch {}
    return Pp.ExitCode;
}

async Task<string> CdpRead(string Body)
{
    var Log = Path.Combine(Path.GetTempPath(), $"out-{Pad}-{Guid.NewGuid():N}.log");
    await Cdp(Body + $"\n        public const string OutputPath = @\"{Log}\";");
    string C = "";
    try { C = await File.ReadAllTextAsync(Log); } catch {}
    try { File.Delete(Log); } catch {}
    return C;
}

async Task<string> WolfsIdx()
{
    var L = await CdpRead("public const string Command = \"list_pages\";");
    foreach (var Ln in L.Split('\n'))
    {
        var T = Ln.Trim();
        if (!T.Contains("wolfstruckingco", StringComparison.OrdinalIgnoreCase)) continue;
        var Colon = T.IndexOf(':');
        if (Colon < 1) continue;
        var Idx = T.Substring(0, Colon).Trim();
        if (Idx.All(char.IsDigit)) return Idx;
    }
    return "1";
}

string CurrentIdx = "1";

async Task<string> Snap()
{
    return await CdpRead("public const string Command = \"take_snapshot\";\n        public const string PageId = \"" + CurrentIdx + "\";");
}

int FindUidByRoleText(string snap, string role, string textPattern)
{
    var Rx = new Regex(@"^\s*\[(\d+)\]\s+" + Regex.Escape(role) + @"\s+""([^""]*)""", RegexOptions.Multiline);
    foreach (Match M in Rx.Matches(snap))
    {
        var Text = M.Groups[2].Value;
        if (Text.Contains(textPattern, StringComparison.OrdinalIgnoreCase)) return int.Parse(M.Groups[1].Value);
    }
    return 0;
}

bool SnapHasText(string snap, string textPattern)
{
    return snap.Contains(textPattern, StringComparison.OrdinalIgnoreCase);
}

async Task ClickUid(int uid)
{
    await Cdp("public const string Command = \"click\";\n        public const string PageId = \"" + CurrentIdx + "\";\n        public const string Uid = \"" + uid + "\";");
}

async Task FillUid(int uid, string val)
{
    await Cdp("public const string Command = \"fill\";\n        public const string PageId = \"" + CurrentIdx + "\";\n        public const string Uid = \"" + uid + "\";\n        public const string Value = \"" + val + "\";");
}

await Cdp("public const string Command = \"new_page\";\n        public const string Url = \"" + Url + "\";");
await Task.Delay(3000);
CurrentIdx = await WolfsIdx();
Console.WriteLine($"scene-{Pad} idx={CurrentIdx} url={Url}");

string ReadySnap = "";
for (int I = 0; I < 30; I++)
{
    ReadySnap = await Snap();
    var ChipUid = FindUidByRoleText(ReadySnap, "button", "Auto");
    if (ChipUid == 0) ChipUid = FindUidByRoleText(ReadySnap, "button", "Light");
    if (ChipUid == 0) ChipUid = FindUidByRoleText(ReadySnap, "button", "Dark");
    if (ChipUid != 0) { Console.WriteLine($"scene-{Pad} wasm-ready: theme chip uid={ChipUid} after {I+1}s"); break; }
    await Task.Delay(1000);
}

Console.WriteLine($"scene-{Pad} waiting 10s for WASM hydration before theme click");
await Task.Delay(10000);
for (int Ti = 0; Ti < 3; Ti++)
{
    var Cur = await Snap();
    var ClickU = FindUidByRoleText(Cur, "button", "Auto");
    if (ClickU == 0) ClickU = FindUidByRoleText(Cur, "button", "Dark");
    if (ClickU == 0) ClickU = FindUidByRoleText(Cur, "button", "Light");
    if (ClickU == 0) { Console.WriteLine($"scene-{Pad} no theme chip in snapshot at iter {Ti}"); break; }
    Console.WriteLine($"scene-{Pad} click chip uid={ClickU} (iter {Ti})");
    await ClickUid(ClickU);
    await Task.Delay(1500);
}

if (RequiresLogin)
{
    var Snap2 = await Snap();
    bool Needs = SnapHasText(Snap2, "Please sign in") || (FindUidByRoleText(Snap2, "link", "Sign In") != 0);
    if (Needs)
    {
        Console.WriteLine($"scene-{Pad} login required (provider={LoginProvider})");
        var SignInUid = FindUidByRoleText(Snap2, "link", "Sign In");
        if (SignInUid == 0) SignInUid = FindUidByRoleText(Snap2, "link", "sign in");
        if (SignInUid != 0)
        {
            await ClickUid(SignInUid);
            await Task.Delay(4000);
            CurrentIdx = await WolfsIdx();
            var Snap3 = await Snap();
            var ProviderUid = FindUidByRoleText(Snap3, "button", LoginProvider);
            if (ProviderUid == 0) ProviderUid = FindUidByRoleText(Snap3, "link", LoginProvider);
            if (ProviderUid != 0)
            {
                await ClickUid(ProviderUid);
                await Task.Delay(8000);
                Console.WriteLine($"scene-{Pad} clicked {LoginProvider} provider — SSO flow needs human ack at password prompt");
            }
        }
    }
}

var Png = Path.Combine(Frames, $"{Pad}.png");
try { File.Delete(Png); } catch {}
await Cdp("public const string Command = \"take_screenshot\";\n        public const string PageId = \"" + CurrentIdx + "\";\n        public const string FilePath = @\"" + Png + "\";");
if (!File.Exists(Png) || new FileInfo(Png).Length == 0) { Console.Error.WriteLine($"scene-{Pad} SCREENSHOT FAIL"); return 3; }
Console.WriteLine($"scene-{Pad} png_size={new FileInfo(Png).Length}");

var Wav = Path.Combine(Audio, $"scene-{Pad}.mp3");
var Mp4 = Path.Combine(Docs, $"scene-{Pad}.mp4");
if (!File.Exists(Wav)) { Console.Error.WriteLine($"scene-{Pad} audio missing: {Wav}"); return 4; }
var Ff = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-y", "-loop", "1", "-i", Png, "-i", Wav, "-c:v", "libx264", "-tune", "stillimage", "-pix_fmt", "yuv420p", "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30", "-c:a", "aac", "-b:a", "128k", "-ar", "44100", "-shortest", Mp4 }) Ff.ArgumentList.Add(A);
using var Fp = Process.Start(Ff)!;
var Oe = Fp.StandardOutput.ReadToEndAsync(); var Ee = Fp.StandardError.ReadToEndAsync(); var Ex = Fp.WaitForExitAsync();
await Task.WhenAny(Ex, Task.Delay(120000));
await Task.WhenAll(Oe, Ee);
Console.WriteLine($"scene-{Pad} mp4_rc={Fp.ExitCode}");
return Fp.ExitCode;
