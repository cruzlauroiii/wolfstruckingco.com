#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
using System.Text;
using System.Text.Json;

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

var ScenesJsonPath = Get("ScenesJsonPath")!;
var OcrDir = Get("OcrDir")!;
var OutputMd = Get("OutputMd")!;

var Json = await File.ReadAllTextAsync(ScenesJsonPath);
using var Doc = JsonDocument.Parse(Json);
var Scenes = Doc.RootElement.EnumerateArray().ToList();

static string Pad(string Url)
{
    var I = Url.IndexOf("cb=", StringComparison.Ordinal);
    if (I < 0) return string.Empty;
    var Start = I + 3;
    var Stop = Start;
    while (Stop < Url.Length && (char.IsDigit(Url[Stop]) || (Url[Stop] >= 'a' && Url[Stop] <= 'z'))) Stop++;
    return Url[Start..Stop];
}

static string[] Words(string Text)
{
    var Sb = new StringBuilder();
    foreach (var Ch in Text)
    {
        if (char.IsLetterOrDigit(Ch)) Sb.Append(char.ToLowerInvariant(Ch));
        else Sb.Append(' ');
    }
    return Sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static int Similarity(string Narration, string Ocr)
{
    var Nw = new HashSet<string>(Words(Narration));
    if (Nw.Count == 0) return 0;
    var Ow = new HashSet<string>(Words(Ocr));
    Nw.IntersectWith(Ow);
    return (int)Math.Round(100.0 * Nw.Count / Math.Max(1, new HashSet<string>(Words(Narration)).Count));
}

var Md = new StringBuilder();
Md.AppendLine("| Scene | URL | Narration | OCR Text | Sim % | Pass/Fail |");
Md.AppendLine("|-------|-----|-----------|----------|-------|-----------|");
foreach (var Scene in Scenes)
{
    var Url = Scene.GetProperty("target").GetString() ?? string.Empty;
    var Narration = Scene.GetProperty("narration").GetString() ?? string.Empty;
    var P = Pad(Url);
    if (string.IsNullOrEmpty(P)) continue;
    var TxtPath = Path.Combine(OcrDir, "scene-" + P + ".txt");
    var Ocr = File.Exists(TxtPath) ? await File.ReadAllTextAsync(TxtPath) : string.Empty;
    Ocr = Ocr.Replace('|', '/').Replace('\n', ' ').Replace('\r', ' ').Trim();
    if (Ocr.Length > 200) Ocr = Ocr[..200] + "...";
    var Sim = Similarity(Narration, Ocr);
    var EscNar = Narration.Replace("|", "/", StringComparison.Ordinal);
    Md.Append("| ").Append(P).Append(" | ").Append(Url).Append(" | ").Append(EscNar).Append(" | ").Append(Ocr).Append(" | ").Append(Sim.ToString(CultureInfo.InvariantCulture)).AppendLine(" |  |");
}
await File.WriteAllTextAsync(OutputMd, Md.ToString());
await Console.Error.WriteLineAsync("narration-vs-ocr: wrote " + OutputMd);
return 0;
