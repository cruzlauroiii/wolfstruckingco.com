#:property TargetFramework=net11.0-windows
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run capture-scene.cs <scratch-config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { Console.Error.WriteLine($"missing: {SpecPath}"); return 2; }
var Spec = await File.ReadAllTextAsync(SpecPath);

string Get(string Name)
{
    var Rx = new Regex("const\\s+string\\s+" + Name + "\\s*=\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\"");
    var M = Rx.Match(Spec);
    return M.Success ? M.Groups[1].Value : "";
}

var Repo = Get("Repo");
if (string.IsNullOrEmpty(Repo)) Repo = @"C:\repo\public\wolfstruckingco.com\main";
var Pad = Get("Pad");
var Url = Get("Url");
var Png = Get("Png");
var Audio = Get("Audio");
var Mp4 = Get("Mp4");
var WaitMs = int.TryParse(Get("WaitMs"), out var W) ? W : 10000;

if (string.IsNullOrEmpty(Pad) || string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(Png) || string.IsNullOrEmpty(Audio) || string.IsNullOrEmpty(Mp4))
{
    Console.Error.WriteLine("missing required keys: Pad, Url, Png, Audio, Mp4");
    return 3;
}

Directory.CreateDirectory(Path.GetDirectoryName(Png)!);
Directory.CreateDirectory(Path.GetDirectoryName(Mp4)!);

async Task<int> Cdp(string Body)
{
    var Tmp = Path.Combine(Path.GetTempPath(), $"cap-{Pad}-{Guid.NewGuid():N}.cs");
    var Cfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        " + Body + "\n    }\n}\n";
    await File.WriteAllTextAsync(Tmp, Cfg);
    var P = new ProcessStartInfo("dotnet") { WorkingDirectory = Repo, RedirectStandardOutput = true, RedirectStandardError = true };
    P.ArgumentList.Add("run");
    P.ArgumentList.Add(Path.Combine(Repo, "scripts", "generic", "chrome-devtools.cs"));
    P.ArgumentList.Add(Tmp);
    using var Pp = Process.Start(P)!;
    var Oe = Pp.StandardOutput.ReadToEndAsync();
    var Ee = Pp.StandardError.ReadToEndAsync();
    var Wt = Pp.WaitForExitAsync();
    if (await Task.WhenAny(Wt, Task.Delay(60000)) != Wt) { try { Pp.Kill(true); } catch {} return -1; }
    await Task.WhenAll(Oe, Ee);
    try { File.Delete(Tmp); } catch {}
    return Pp.ExitCode;
}

async Task<string> CdpRead(string Body)
{
    var Log = Path.Combine(Path.GetTempPath(), $"capout-{Pad}-{Guid.NewGuid():N}.log");
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
        if (!T.Contains("wolfstruckingco", StringComparison.OrdinalIgnoreCase) && !T.Contains("localhost", StringComparison.OrdinalIgnoreCase) && !T.Contains("Documents", StringComparison.OrdinalIgnoreCase)) continue;
        var Colon = T.IndexOf(':');
        if (Colon < 1) continue;
        var Idx = T.Substring(0, Colon).Trim();
        if (Idx.All(char.IsDigit)) return Idx;
    }
    return "1";
}

await Cdp("public const string Command = \"new_page\";\n        public const string Url = \"" + Url + "\";");
await Task.Delay(WaitMs);
var Idx = await WolfsIdx();
try { File.Delete(Png); } catch {}
await Cdp("public const string Command = \"take_screenshot\";\n        public const string PageId = \"" + Idx + "\";\n        public const string FilePath = @\"" + Png + "\";");
if (!File.Exists(Png) || new FileInfo(Png).Length == 0) { Console.Error.WriteLine($"capture-{Pad} SCREENSHOT FAIL"); return 4; }

if (!File.Exists(Audio)) { Console.Error.WriteLine($"capture-{Pad} audio missing: {Audio}"); return 5; }
try { File.Delete(Mp4); } catch {}
var Ff = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-y", "-loop", "1", "-i", Png, "-i", Audio, "-c:v", "libx264", "-tune", "stillimage", "-pix_fmt", "yuv420p", "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30", "-c:a", "aac", "-b:a", "128k", "-ar", "44100", "-shortest", Mp4 }) Ff.ArgumentList.Add(A);
using var Fp = Process.Start(Ff)!;
var Fo = Fp.StandardOutput.ReadToEndAsync();
var Fe = Fp.StandardError.ReadToEndAsync();
var Fx = Fp.WaitForExitAsync();
if (await Task.WhenAny(Fx, Task.Delay(180000)) != Fx) { try { Fp.Kill(true); } catch {} Console.Error.WriteLine($"capture-{Pad} ffmpeg timeout"); return 6; }
await Task.WhenAll(Fo, Fe);
if (Fp.ExitCode != 0) { Console.Error.WriteLine($"capture-{Pad} ffmpeg rc={Fp.ExitCode}"); return Fp.ExitCode; }
if (!File.Exists(Mp4) || new FileInfo(Mp4).Length == 0) { Console.Error.WriteLine($"capture-{Pad} MP4 missing"); return 7; }
return 0;
