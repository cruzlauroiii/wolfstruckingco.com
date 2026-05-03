#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;
using System.Text;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var Specs = await File.ReadAllLinesAsync(SpecPath);
string? Read(string Name)
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

var ClipDir = Read("ClipDir");
var OutputPath = Read("OutputPath");
var FfmpegPath = Read("FfmpegPath") ?? "ffmpeg";
var FfprobePath = Read("FfprobePath") ?? "ffprobe";

if (ClipDir is null || OutputPath is null) return 3;
if (!Directory.Exists(ClipDir)) return 4;

var Clips = Directory.EnumerateFiles(ClipDir, "*.mp4").OrderBy(P => P, StringComparer.Ordinal).ToArray();
if (Clips.Length == 0) return 5;

var ConcatList = Path.Combine(ClipDir, "concat.txt");
var Sb = new StringBuilder();
foreach (var Clip in Clips)
{
    var Esc = Clip.Replace("\\", "/", StringComparison.Ordinal).Replace("'", "'\\''", StringComparison.Ordinal);
    Sb.Append("file '");
    Sb.Append(Esc);
    Sb.Append("'\n");
}
await File.WriteAllTextAsync(ConcatList, Sb.ToString());

var OutDir = Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(OutDir) && !Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);

var Psi = new ProcessStartInfo(FfmpegPath) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-y", "-f", "concat", "-safe", "0", "-i", ConcatList, "-c", "copy", OutputPath }) Psi.ArgumentList.Add(A);
using var P = Process.Start(Psi)!;
var KeywordRe = new System.Text.RegularExpressions.Regex("\\b(warning|error)\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
var Killed = false;
string? OffendingLine = null;
async Task StreamLines(StreamReader Sr)
{
    string? Ln;
    while ((Ln = await Sr.ReadLineAsync()) is not null)
    {
        if (KeywordRe.IsMatch(Ln) && !Killed)
        {
            Killed = true; OffendingLine = Ln;
            try { P.Kill(true); } catch { }
            return;
        }
    }
}
var T1 = StreamLines(P.StandardOutput);
var T2 = StreamLines(P.StandardError);
await P.WaitForExitAsync();
await Task.WhenAll(T1, T2);
if (Killed) { await Console.Error.WriteLineAsync($"ffmpeg warning/error: {OffendingLine?.Trim()}"); return 6; }
if (P.ExitCode != 0) return 6;
if (!File.Exists(OutputPath)) return 7;
return 0;
