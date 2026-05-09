#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

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

var Mp4Dir = Get("Mp4Dir")!;
var OutDir = Get("OutDir")!;
Directory.CreateDirectory(OutDir);

var Mp4s = Directory.GetFiles(Mp4Dir, "scene-*.mp4");
Array.Sort(Mp4s, StringComparer.Ordinal);
var Done = 0;
var Skipped = 0;
foreach (var Mp4 in Mp4s)
{
    var Name = Path.GetFileNameWithoutExtension(Mp4);
    var Png = Path.Combine(OutDir, Name + ".png");
    if (File.Exists(Png)) { Skipped++; continue; }
    var Psi = new ProcessStartInfo("ffmpeg")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        ArgumentList = { "-y", "-i", Mp4, "-vframes", "1", "-q:v", "2", Png },
    };
    using var P = Process.Start(Psi)!;
    _ = await P.StandardError.ReadToEndAsync();
    _ = await P.StandardOutput.ReadToEndAsync();
    await P.WaitForExitAsync();
    if (P.ExitCode == 0 && File.Exists(Png)) Done++;
}
await Console.Error.WriteLineAsync("extract-first-frames: done=" + Done.ToString(System.Globalization.CultureInfo.InvariantCulture) + " skipped=" + Skipped.ToString(System.Globalization.CultureInfo.InvariantCulture));
return 0;
