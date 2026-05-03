#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;
using System.Text.Json;

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

var AudioIndexPath = Read("AudioIndexPath");
var FrameDir = Read("FrameDir");
var AudioDir = Read("AudioDir");
var ClipDir = Read("ClipDir");
var FfmpegPath = Read("FfmpegPath") ?? "ffmpeg";

if (AudioIndexPath is null || FrameDir is null || AudioDir is null || ClipDir is null) return 3;
if (!File.Exists(AudioIndexPath)) return 4;
Directory.CreateDirectory(ClipDir);
foreach (var F in Directory.GetFiles(ClipDir, "*.mp4")) File.Delete(F);

var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(AudioIndexPath));
var Items = Doc.RootElement.EnumerateArray().ToArray();
var Made = 0;
var Failed = 0;

foreach (var Item in Items)
{
    var Pad = Item.GetProperty("pad").GetString() ?? "";
    var Ok = Item.GetProperty("ok").GetBoolean();
    if (!Ok) { Failed++; continue; }
    var Frame = Path.Combine(FrameDir, $"{Pad}.png");
    var Wav = Path.Combine(AudioDir, $"{Pad}.wav");
    var Clip = Path.Combine(ClipDir, $"{Pad}.mp4");
    if (!File.Exists(Frame)) { Failed++; continue; }
    if (!File.Exists(Wav)) { Failed++; continue; }

    var ArgsList = new List<string> { "-y", "-loop", "1", "-i", Frame, "-i", Wav, "-c:v", "libx264", "-tune", "stillimage", "-pix_fmt", "yuv420p", "-vf", "scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2:black", "-c:a", "aac", "-b:a", "192k", "-shortest", Clip };
    var Psi = new ProcessStartInfo(FfmpegPath) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var A in ArgsList) Psi.ArgumentList.Add(A);
    try
    {
        using var P = Process.Start(Psi)!;
        var KeywordRe = new System.Text.RegularExpressions.Regex("\\b(warning|error)\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var Killed = false;
        string? OffendingLine = null;
        async Task StreamReader_(StreamReader Sr)
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
        var T1 = StreamReader_(P.StandardOutput);
        var T2 = StreamReader_(P.StandardError);
        await P.WaitForExitAsync();
        await Task.WhenAll(T1, T2);
        if (Killed) { await Console.Error.WriteLineAsync($"clip {Pad} warning/error: {OffendingLine?.Trim()}"); return 7; }
        if (P.ExitCode == 0 && File.Exists(Clip)) { Made++; }
        else { Failed++; }
    }
    catch { Failed++; }
}
if (Made == 0) return 5;
if (Failed > 0) return 6;
return 0;
