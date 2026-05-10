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

var SrcDir = Get("SrcDir")!;
var DstDir = Get("DstDir")!;
var Pattern = Get("Pattern") ?? "scene-*.mp4";
Directory.CreateDirectory(DstDir);

var Mp4s = Directory.EnumerateFiles(SrcDir, Pattern).OrderBy(f => f).ToList();
var Done = 0;
var Skip = 0;
var Fail = 0;
foreach (var Mp4 in Mp4s)
{
    var Name = Path.GetFileNameWithoutExtension(Mp4);
    if (Name.EndsWith("a", StringComparison.Ordinal)) continue;
    var OutMp3 = Path.Combine(DstDir, Name + ".mp3");
    if (File.Exists(OutMp3) && new FileInfo(OutMp3).Length > 0) { Skip++; continue; }
    var Psi = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var A in new[] { "-y", "-i", Mp4, "-vn", "-acodec", "libmp3lame", "-q:a", "4", OutMp3 }) Psi.ArgumentList.Add(A);
    Psi.WorkingDirectory = Path.GetTempPath();
    using var P = Process.Start(Psi)!;
    var O = P.StandardOutput.ReadToEndAsync();
    var E = P.StandardError.ReadToEndAsync();
    var X = P.WaitForExitAsync();
    var Win = await Task.WhenAny(X, Task.Delay(30000));
    if (Win != X) { try { P.Kill(true); } catch { } Fail++; continue; }
    var ErrText = await E;
    if (P.ExitCode == 0 && File.Exists(OutMp3) && new FileInfo(OutMp3).Length > 0) Done++;
    else { Fail++; if (Fail <= 3) await Console.Error.WriteLineAsync("FAIL " + Name + " exit=" + P.ExitCode + " stderr=" + (ErrText.Length > 400 ? ErrText[(ErrText.Length-400)..] : ErrText)); }
}
await Console.Error.WriteLineAsync("extract-audio: done=" + Done.ToString(CultureInfo.InvariantCulture) + " skip=" + Skip.ToString(CultureInfo.InvariantCulture) + " fail=" + Fail.ToString(CultureInfo.InvariantCulture));
return 0;
