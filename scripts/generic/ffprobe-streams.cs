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
var Pattern = Get("Pattern") ?? "*.mp4";
var Limit = int.Parse(Get("Limit") ?? "10", CultureInfo.InvariantCulture);

var Files = Directory.EnumerateFiles(Dir, Pattern).OrderBy(f => f).Take(Limit).ToList();
await Console.Error.WriteLineAsync("ffprobe-streams: count=" + Files.Count.ToString(CultureInfo.InvariantCulture));
foreach (var F in Files)
{
    var Psi = new ProcessStartInfo("ffprobe") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    foreach (var A in new[] { "-v", "error", "-show_entries", "stream=codec_type,codec_name,duration", "-of", "csv=p=0", F }) Psi.ArgumentList.Add(A);
    using var P = Process.Start(Psi)!;
    var Out = await P.StandardOutput.ReadToEndAsync();
    await P.WaitForExitAsync();
    var Streams = Out.Trim().Replace('\n', '|').Replace("\r", "");
    await Console.Error.WriteLineAsync("  " + Path.GetFileName(F) + " (" + new FileInfo(F).Length + "B): " + Streams);
}
return 0;
