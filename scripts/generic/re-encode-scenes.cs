#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string? Get(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        if (After.StartsWith("@", StringComparison.Ordinal)) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}

var FramesDir = Get("FramesDir") ?? "";
var AudioDir = Get("AudioDir") ?? "";
var OutDir = Get("OutDir") ?? "";
if (string.IsNullOrEmpty(FramesDir) || string.IsNullOrEmpty(AudioDir) || string.IsNullOrEmpty(OutDir)) return 3;
Directory.CreateDirectory(OutDir);

var Pngs = Directory.GetFiles(FramesDir, "*.png").OrderBy(f => f).ToList();
var Ok = 0;
var Fail = new List<string>();
foreach (var Png in Pngs)
{
    var Pad = Path.GetFileNameWithoutExtension(Png);
    var Mp3 = Path.Combine(AudioDir, $"scene-{Pad}.mp3");
    var Mp4 = Path.Combine(OutDir, $"scene-{Pad}.mp4");
    if (!File.Exists(Mp3)) { Fail.Add(Pad + ":mp3-missing"); continue; }
    var Psi = new ProcessStartInfo("ffmpeg") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var A in new[] { "-y", "-loop", "1", "-i", Png, "-i", Mp3,
        "-c:v", "libx264", "-tune", "stillimage", "-pix_fmt", "yuv420p",
        "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30",
        "-c:a", "aac", "-b:a", "128k", "-ar", "44100", "-shortest", Mp4 }) Psi.ArgumentList.Add(A);
    using var P = Process.Start(Psi)!;
    var Ot = P.StandardOutput.ReadToEndAsync();
    var Et = P.StandardError.ReadToEndAsync();
    var ExitTask = P.WaitForExitAsync();
    var Done = await Task.WhenAny(ExitTask, Task.Delay(120000));
    if (Done != ExitTask) { try { P.Kill(true); } catch { } Fail.Add(Pad + ":timeout"); continue; }
    await Task.WhenAll(Ot, Et);
    if (P.ExitCode != 0) { Fail.Add(Pad + ":rc=" + P.ExitCode); continue; }
    Ok++;
    Console.WriteLine($"  {Pad} OK");
}
Console.WriteLine($"DONE ok={Ok} fail={Fail.Count}");
return Fail.Count == 0 ? 0 : 5;
