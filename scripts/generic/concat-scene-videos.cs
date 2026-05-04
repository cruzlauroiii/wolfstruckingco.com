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
    var match = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return match.Success ? match.Groups["v"].Value : fallback;
}

var docs = Get("Docs");
var audio = Get("Audio");
var output = Get("OutputPath", Path.Combine(docs, "walkthrough.mp4"));
var count = int.Parse(Get("SceneCount", "121"));
if (!Directory.Exists(docs)) return 2;
if (!Directory.Exists(audio)) return 4;

var tempDir = Path.Combine(Path.GetTempPath(), "wolf-normalized-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempDir);
var listPath = Path.Combine(Path.GetTempPath(), "wolf-scenes-" + Guid.NewGuid().ToString("N") + ".txt");
var lines = new List<string>();

async Task<int> RunFfmpeg(params string[] args)
{
    var start = new ProcessStartInfo("ffmpeg")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    start.ArgumentList.Add("-hide_banner");
    start.ArgumentList.Add("-loglevel");
    start.ArgumentList.Add("error");
    foreach (var arg in args) start.ArgumentList.Add(arg);
    using var child = Process.Start(start)!;
    var stdout = child.StandardOutput.ReadToEndAsync();
    var stderr = child.StandardError.ReadToEndAsync();
    var wait = child.WaitForExitAsync();
    if (await Task.WhenAny(wait, Task.Delay(300000)) != wait)
    {
        child.Kill(true);
        return 124;
    }
    var outText = await stdout;
    var errText = await stderr;
    if (!string.IsNullOrWhiteSpace(outText)) Console.Write(outText);
    if (!string.IsNullOrWhiteSpace(errText)) Console.Error.Write(errText);
    return child.ExitCode;
}

for (var i = 1; i <= count; i++)
{
    var path = Path.Combine(docs, $"scene-{i:000}.mp4");
    var audioPath = Path.Combine(audio, $"scene-{i:000}.mp3");
    var normalized = Path.Combine(tempDir, $"scene-{i:000}.mp4");
    if (!File.Exists(path))
    {
        Console.Error.WriteLine("Missing scene: " + path);
        return 3;
    }
    if (!File.Exists(audioPath))
    {
        Console.Error.WriteLine("Missing audio: " + audioPath);
        return 5;
    }
    var rc = await RunFfmpeg(
        "-y", "-i", path, "-i", audioPath,
        "-map", "0:v:0", "-map", "1:a:0",
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-preset", "veryfast",
        "-c:a", "aac", "-b:a", "128k", "-ar", "44100",
        "-shortest", normalized);
    if (rc != 0) return rc;
    lines.Add("file '" + normalized.Replace("'", "'\\''") + "'");
}
await File.WriteAllLinesAsync(listPath, lines);

var concatRc = await RunFfmpeg("-y", "-f", "concat", "-safe", "0", "-i", listPath, "-c", "copy", output);
Console.WriteLine("Full video: " + output);
return concatRc;
