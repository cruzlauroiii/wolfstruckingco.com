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

var ScenesDir = Get("ScenesDir") ?? "";
var OutPath = Get("OutPath") ?? "";
if (string.IsNullOrEmpty(ScenesDir) || string.IsNullOrEmpty(OutPath)) return 3;

var Mp4s = Directory.GetFiles(ScenesDir, "scene-*.mp4").OrderBy(f => f).ToList();
if (Mp4s.Count == 0) return 4;

var ConcatTxt = Path.Combine(Path.GetTempPath(), "concat-walkthrough.txt");
await File.WriteAllLinesAsync(ConcatTxt, Mp4s.Select(m => $"file '{m.Replace("\\", "/")}'"));

try { File.Delete(OutPath); } catch { }
var Psi = new ProcessStartInfo("ffmpeg") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-y", "-f", "concat", "-safe", "0", "-i", ConcatTxt, "-c", "copy", OutPath }) Psi.ArgumentList.Add(A);
using var P = Process.Start(Psi)!;
var Ot = P.StandardOutput.ReadToEndAsync();
var Et = P.StandardError.ReadToEndAsync();
var ExitTask = P.WaitForExitAsync();
var Done = await Task.WhenAny(ExitTask, Task.Delay(600000));
if (Done != ExitTask) { try { P.Kill(true); } catch { } return 5; }
await Task.WhenAll(Ot, Et);
Console.WriteLine($"concat rc={P.ExitCode} -> {OutPath} ({(File.Exists(OutPath) ? new FileInfo(OutPath).Length : 0)} bytes)");
return P.ExitCode;
