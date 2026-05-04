#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name, string fallback = "")
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : fallback;
}

var frames = Get("FrameDir");
var audio = Get("AudioDir");
var docs = Get("DocsDir");
var count = int.Parse(Get("SceneCount", "121"));
if (!Directory.Exists(frames) || !Directory.Exists(audio)) return 2;
Directory.CreateDirectory(docs);

async Task<int> Ffmpeg(params string[] xs)
{
    var psi = new ProcessStartInfo("ffmpeg")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    foreach (var a in new[] { "-hide_banner", "-loglevel", "error" }) psi.ArgumentList.Add(a);
    foreach (var a in xs) psi.ArgumentList.Add(a);
    using var p = Process.Start(psi) ?? throw new InvalidOperationException("ffmpeg failed");
    var stdout = p.StandardOutput.ReadToEndAsync();
    var stderr = p.StandardError.ReadToEndAsync();
    var wait = p.WaitForExitAsync();
    if (await Task.WhenAny(wait, Task.Delay(180000)) != wait)
    {
        p.Kill(entireProcessTree: true);
        return 124;
    }
    var outText = await stdout;
    var errText = await stderr;
    if (!string.IsNullOrWhiteSpace(outText)) Console.Write(outText);
    if (!string.IsNullOrWhiteSpace(errText)) Console.Error.Write(errText);
    return p.ExitCode;
}

for (var i = 1; i <= count; i++)
{
    var pad = i.ToString("000", System.Globalization.CultureInfo.InvariantCulture);
    var frame = Path.Combine(frames, pad + ".png");
    var mp3 = Path.Combine(audio, "scene-" + pad + ".mp3");
    var mp4 = Path.Combine(docs, "scene-" + pad + ".mp4");
    if (!File.Exists(frame)) { Console.Error.WriteLine("Missing frame: " + frame); return 3; }
    if (!File.Exists(mp3)) { Console.Error.WriteLine("Missing audio: " + mp3); return 4; }
    var rc = await Ffmpeg(
        "-y", "-loop", "1", "-i", frame, "-i", mp3,
        "-c:v", "libx264", "-tune", "stillimage", "-pix_fmt", "yuv420p",
        "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2:black,fps=30",
        "-c:a", "aac", "-b:a", "128k", "-ar", "44100", "-shortest", mp4);
    if (rc != 0) return rc;
    Console.WriteLine("encoded " + mp4);
}
return 0;
