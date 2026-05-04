#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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

var ScenesPath = Get("ScenesPath") ?? "";
var OcrJsonPath = Get("OcrJsonPath") ?? "";
var OutputPath = Get("OutputPath") ?? "";
if (!File.Exists(ScenesPath) || !File.Exists(OcrJsonPath) || string.IsNullOrEmpty(OutputPath)) return 3;

var ScenesDoc = JsonDocument.Parse(await File.ReadAllTextAsync(ScenesPath));
var OcrDoc = JsonDocument.Parse(await File.ReadAllTextAsync(OcrJsonPath));
var OcrByPad = new Dictionary<string, string>();
foreach (var F in OcrDoc.RootElement.GetProperty("frames").EnumerateArray())
{
    var P = F.GetProperty("pad").GetString() ?? "";
    var T = F.TryGetProperty("text", out var Tp) ? Tp.GetString() ?? "" : "";
    OcrByPad[P] = T;
}

string PadFor(JsonElement Sc, int Idx)
{
    var T = Sc.GetProperty("target").GetString() ?? "";
    var M = Regex.Match(T, "cb=(\\d+)");
    return M.Success ? M.Groups[1].Value.PadLeft(3, '0') : Idx.ToString("D3");
}

double Similarity(string A, string B)
{
    var Aw = Regex.Split(A.ToLowerInvariant(), "\\W+").Where(w => w.Length > 2).ToHashSet();
    var Bw = Regex.Split(B.ToLowerInvariant(), "\\W+").Where(w => w.Length > 2).ToHashSet();
    if (Aw.Count == 0 || Bw.Count == 0) return 0;
    var Inter = Aw.Intersect(Bw).Count();
    var Union = Aw.Union(Bw).Count();
    return (double)Inter / Union;
}

var Md = new StringBuilder();
Md.AppendLine("# Narration vs OCR Audit");
Md.AppendLine();
Md.AppendLine("Per-scene comparison of intended narration against OCR-extracted on-screen text.");
Md.AppendLine();
Md.AppendLine("| Pad | Score | Narration | OCR (first 80c) | Verdict |");
Md.AppendLine("|-----|-------|-----------|-----------------|---------|");

var Idx0 = 0;
foreach (var Sc in ScenesDoc.RootElement.EnumerateArray())
{
    Idx0++;
    var Pad = PadFor(Sc, Idx0);
    var Narration = Sc.GetProperty("narration").GetString() ?? "";
    OcrByPad.TryGetValue(Pad, out var OcrText);
    OcrText ??= "";
    var Score = Similarity(Narration, OcrText);
    var Verdict = Score >= 0.4 ? "ok" : Score >= 0.15 ? "weak" : "mismatch";
    var NarrEsc = Narration.Replace("|", "\\|");
    var OcrEsc = (OcrText.Length > 80 ? OcrText[..80] : OcrText).Replace("|", "\\|").Replace("\n", " ");
    Md.AppendLine($"| {Pad} | {Score:F2} | {NarrEsc} | {OcrEsc} | {Verdict} |");
}

var OutDir = Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(OutDir)) Directory.CreateDirectory(OutDir);
await File.WriteAllTextAsync(OutputPath, Md.ToString());
return 0;
