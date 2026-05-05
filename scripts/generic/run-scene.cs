#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run run-scene.cs scene-NNN-config.cs"); return 1; }
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
var HydrateSelector = Get("HydrateSelector"); if (string.IsNullOrEmpty(HydrateSelector)) HydrateSelector = ".TopBar";
var BeforeShotJs = Get("BeforeShotJs");
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

await Cdp("public const string Command = \"new_page\";\n        public const string Url = \"" + Url + "\";");
await Task.Delay(3000);
var Idx = await WolfsIdx();
Console.WriteLine($"scene-{Pad} idx={Idx} url={Url}");

var Esc = HydrateSelector.Replace("\"", "\\\"");
for (int I = 0; I < 30; I++)
{
    var R = await CdpRead("public const string Command = \"evaluate_script\";\n        public const string PageId = \"" + Idx + "\";\n        public const string Function = \"() => document.querySelector('" + Esc.Replace("'", "\\'") + "') ? 'ready' : 'wait'\";");
    if (R.Contains("\"ready\"")) { Console.WriteLine($"scene-{Pad} hydrated after {I+1}s"); break; }
    await Task.Delay(1000);
}

if (!string.IsNullOrEmpty(BeforeShotJs))
{
    await Cdp("public const string Command = \"evaluate_script\";\n        public const string PageId = \"" + Idx + "\";\n        public const string Function = \"" + BeforeShotJs + "\";");
    await Task.Delay(4000);
    Idx = await WolfsIdx();
    for (int I = 0; I < 30; I++)
    {
        var R2 = await CdpRead("public const string Command = \"evaluate_script\";\n        public const string PageId = \"" + Idx + "\";\n        public const string Function = \"() => document.querySelector('" + Esc.Replace("'", "\\'") + "') ? 'ready' : 'wait'\";");
        if (R2.Contains("\"ready\"")) { Console.WriteLine($"scene-{Pad} re-hydrated after BeforeShotJs in {I+1}s"); break; }
        await Task.Delay(1000);
    }
}
await Cdp("public const string Command = \"evaluate_script\";\n        public const string PageId = \"" + Idx + "\";\n        public const string Function = \"() => { document.documentElement.setAttribute('data-theme','light'); return 'ok'; }\";");

var Png = Path.Combine(Frames, $"{Pad}.png");
try { File.Delete(Png); } catch {}
await Cdp("public const string Command = \"take_screenshot\";\n        public const string PageId = \"" + Idx + "\";\n        public const string FilePath = @\"" + Png + "\";");
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
Console.WriteLine($"scene-{Pad} mp4_rc={Fp.ExitCode} mp4={Mp4}");
return Fp.ExitCode;
