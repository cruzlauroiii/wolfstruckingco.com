#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != '\u0022') continue;
        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var Dir = Get("Dir")!;
var Pattern = Get("Pattern") ?? "scene-*.mp4";
var Files = Directory.EnumerateFiles(Dir, Pattern).OrderBy(f => f).ToList();
var Deleted = 0;
var Kept = 0;
foreach (var F in Files)
{
    var Psi = new ProcessStartInfo("ffprobe") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    foreach (var A in new[] { "-v", "error", "-select_streams", "a", "-show_entries", "stream=codec_name", "-of", "csv=p=0", F }) Psi.ArgumentList.Add(A);
    using var P = Process.Start(Psi)!;
    var Out = await P.StandardOutput.ReadToEndAsync();
    await P.WaitForExitAsync();
    if (string.IsNullOrWhiteSpace(Out))
    {
        File.Delete(F);
        Deleted++;
    }
    else
    {
        Kept++;
    }
}
await Console.Error.WriteLineAsync("delete-mp4s-without-audio: deleted=" + Deleted.ToString(CultureInfo.InvariantCulture) + " kept=" + Kept.ToString(CultureInfo.InvariantCulture));
return 0;
