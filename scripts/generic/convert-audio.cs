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

var SourceDir = Get("SourceDir") ?? "";
var TargetDir = Get("TargetDir") ?? "";
var SourceExt = Get("SourceExt") ?? ".wav";
var TargetExt = Get("TargetExt") ?? ".mp3";
if (string.IsNullOrEmpty(SourceDir) || string.IsNullOrEmpty(TargetDir) || !Directory.Exists(SourceDir)) return 3;
Directory.CreateDirectory(TargetDir);

var Files = Directory.GetFiles(SourceDir, "*" + SourceExt).OrderBy(f => f).ToList();
var Ok = 0;
var Fail = new List<string>();
foreach (var F in Files)
{
    var Name = Path.GetFileNameWithoutExtension(F);
    var Out = Path.Combine(TargetDir, Name + TargetExt);
    var Psi = new ProcessStartInfo("ffmpeg") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var A in new[] { "-y", "-i", F, "-codec:a", "libmp3lame", "-b:a", "128k", Out }) Psi.ArgumentList.Add(A);
    using var P = Process.Start(Psi)!;
    var Ot = P.StandardOutput.ReadToEndAsync();
    var Et = P.StandardError.ReadToEndAsync();
    var ExitTask = P.WaitForExitAsync();
    var Done = await Task.WhenAny(ExitTask, Task.Delay(60000));
    if (Done != ExitTask) { try { P.Kill(true); } catch { } Fail.Add(Name + ":timeout"); continue; }
    await Task.WhenAll(Ot, Et);
    if (P.ExitCode != 0) { Fail.Add(Name + ":rc=" + P.ExitCode); continue; }
    Ok++;
}
Console.WriteLine($"DONE ok={Ok} fail={Fail.Count} {string.Join(",", Fail)}");
return Fail.Count == 0 ? 0 : 5;
