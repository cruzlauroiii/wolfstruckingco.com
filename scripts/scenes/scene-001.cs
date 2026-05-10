#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

var Repo = @"C:\repo\public\wolfstruckingco.com\main";
var Frames = @"C:\Users\user1\AppData\Local\Temp\wolfs-frames";
var Audio = @"C:\Users\user1\AppData\Local\Temp\wolfs-video\audio-edge";
var Docs = @"C:\repo\public\wolfstruckingco.com\main\docs\videos";
var Url = "https://cruzlauroiii.github.io/wolfstruckingco.com/?cb=001";
var Pad = "001";
Directory.CreateDirectory(Frames);

async Task<int> Cdp(string ScratchBody)
{
    var Tmp = Path.Combine(Path.GetTempPath(), $"cdp-{Pad}-{Guid.NewGuid():N}.cs");
    var Cfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        " + ScratchBody + "\n    }\n}\n";
    await File.WriteAllTextAsync(Tmp, Cfg);
    var Psi = new ProcessStartInfo("dotnet") { WorkingDirectory = Repo, RedirectStandardOutput = true, RedirectStandardError = true };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(Path.Combine(Repo, "scripts", "generic", "chrome-devtools.cs"));
    Psi.ArgumentList.Add(Tmp);
    using var P = Process.Start(Psi)!;
    var Wait = P.WaitForExitAsync();
    if (await Task.WhenAny(Wait, Task.Delay(60000)) != Wait) { try { P.Kill(true); } catch {} return -1; }
    try { File.Delete(Tmp); } catch {}
    return P.ExitCode;
}

async Task<string> CdpRead(string ScratchBody)
{
    var Log = Path.Combine(Path.GetTempPath(), $"out-{Pad}-{Guid.NewGuid():N}.log");
    var Body = ScratchBody + $"\n        public const string OutputPath = @\"{Log}\";";
    await Cdp(Body);
    string C = "";
    try { C = await File.ReadAllTextAsync(Log); } catch {}
    try { File.Delete(Log); } catch {}
    return C;
}

async Task<string> WolfsPageIdx()
{
    var Listing = await CdpRead("public const string Command = \"list_pages\";");
    foreach (var Ln in Listing.Split('\n'))
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
var PageIdx = await WolfsPageIdx();
Console.WriteLine($"scene-{Pad} wolfs page at idx={PageIdx}");

for (int I = 0; I < 20; I++)
{
    var R = await CdpRead("public const string Command = \"evaluate_script\";\n        public const string PageId = \"" + PageIdx + "\";\n        public const string Function = \"() => { var t = document.querySelector('.TopBar'); if (!t) return 'no-topbar'; var hasAuth = t.querySelector('.LinkBtn,a[href*=Login],a[href*=login]'); return hasAuth ? 'ready' : 'no-auth-yet'; }\";");
    if (R.Contains("\"ready\"")) { Console.WriteLine($"scene-{Pad} hydrated after {I+1}s"); break; }
    await Task.Delay(1000);
}

await CdpRead("public const string Command = \"evaluate_script\";\n        public const string PageId = \"" + PageIdx + "\";\n        public const string Function = \"() => { var btns = Array.from(document.querySelectorAll('.TopBar button, .TopBar .LinkBtn')); for (var b of btns) { if (/log\\\\s*off/i.test(b.textContent||'')) { b.click(); return 'clicked'; } } return 'no-button'; }\";");
await Task.Delay(3000);
await Cdp("public const string Command = \"evaluate_script\";\n        public const string PageId = \"" + PageIdx + "\";\n        public const string Function = \"() => { document.documentElement.setAttribute('data-theme','light'); return 'ok'; }\";");

var Png = Path.Combine(Frames, $"{Pad}.png");
try { File.Delete(Png); } catch {}
await Cdp("public const string Command = \"take_screenshot\";\n        public const string PageId = \"" + PageIdx + "\";\n        public const string FilePath = @\"" + Png + "\";");
if (!File.Exists(Png) || new FileInfo(Png).Length == 0) { Console.Error.WriteLine($"scene-{Pad} SCREENSHOT FAIL"); return 2; }
Console.WriteLine($"scene-{Pad} png={Png} size={new FileInfo(Png).Length}");

var Wav = Path.Combine(Audio, $"scene-{Pad}.mp3");
var Mp4 = Path.Combine(Docs, $"scene-{Pad}.mp4");
if (!File.Exists(Wav)) { Console.Error.WriteLine($"scene-{Pad} audio missing: {Wav}"); return 3; }
var Ff = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-y", "-loop", "1", "-i", Png, "-i", Wav, "-c:v", "libx264", "-tune", "stillimage", "-pix_fmt", "yuv420p", "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30", "-c:a", "aac", "-b:a", "128k", "-ar", "44100", "-shortest", Mp4 }) Ff.ArgumentList.Add(A);
using var Fp = Process.Start(Ff)!;
var O = Fp.StandardOutput.ReadToEndAsync(); var E = Fp.StandardError.ReadToEndAsync(); var Ex = Fp.WaitForExitAsync();
await Task.WhenAny(Ex, Task.Delay(120000));
await Task.WhenAll(O, E);
Console.WriteLine($"scene-{Pad} mp4={Mp4} ffmpeg_rc={Fp.ExitCode}");
return Fp.ExitCode;
