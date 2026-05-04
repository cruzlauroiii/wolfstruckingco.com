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

var frameDir = Get("FrameDir");
var outputDir = Get("OutputDir");
var perSheet = int.Parse(Get("PerSheet", "20"));
if (!Directory.Exists(frameDir)) return 2;
Directory.CreateDirectory(outputDir);
var files = Directory.GetFiles(frameDir, "*.png").OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
for (var offset = 0; offset < files.Length; offset += perSheet)
{
    var chunk = files.Skip(offset).Take(perSheet).ToArray();
    var outPath = Path.Combine(outputDir, $"sheet-{offset / perSheet + 1:00}.jpg");
    var listPath = Path.Combine(Path.GetTempPath(), "sheet-" + Guid.NewGuid().ToString("N") + ".txt");
    await File.WriteAllLinesAsync(listPath, chunk.Select(x => "file '" + x.Replace("'", "'\\''", StringComparison.Ordinal) + "'"));
    var psi = new ProcessStartInfo("ffmpeg")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    foreach (var a in new[] { "-hide_banner", "-loglevel", "error", "-y", "-f", "concat", "-safe", "0", "-i", listPath, "-vf", "scale=384:216,tile=3x4", "-frames:v", "1", outPath }) psi.ArgumentList.Add(a);
    using var p = Process.Start(psi) ?? throw new InvalidOperationException("magick failed");
    await p.WaitForExitAsync();
    if (p.ExitCode != 0) return p.ExitCode;
    Console.WriteLine(outPath);
}
return 0;
