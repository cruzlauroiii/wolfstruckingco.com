#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Net.Http;
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
var WorkerUrl = Get("WorkerUrl") ?? "";
var OutputPath = Get("OutputPath") ?? "";
var SessionId = Get("SessionId") ?? "narration-audit";
var Threshold = double.Parse(Get("Threshold") ?? "0.4", System.Globalization.CultureInfo.InvariantCulture);
if (!File.Exists(ScenesPath) || !File.Exists(OcrJsonPath) || string.IsNullOrEmpty(WorkerUrl) || string.IsNullOrEmpty(OutputPath)) return 3;

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

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
var Md = new StringBuilder();
Md.AppendLine("# Narration LLM Audit");
Md.AppendLine();
Md.AppendLine("LLM-rewritten narrations for scenes whose OCR diverges from the intended text.");
Md.AppendLine();
Md.AppendLine("| Pad | Jaccard | LLM Score | Original | Suggested Rewrite |");
Md.AppendLine("|-----|---------|-----------|----------|-------------------|");

var Idx0 = 0;
var Reviewed = 0;
var Errors = 0;
foreach (var Sc in ScenesDoc.RootElement.EnumerateArray())
{
    Idx0++;
    var Pad = PadFor(Sc, Idx0);
    var Narration = Sc.GetProperty("narration").GetString() ?? "";
    OcrByPad.TryGetValue(Pad, out var OcrText);
    OcrText ??= "";
    var Score = Similarity(Narration, OcrText);
    if (Score >= Threshold) continue;

    var SystemPrompt = "You are the Wolfs Trucking dispatch QA assistant. You audit how well our recorded walkthrough narrations match the on-screen UI text we OCR'd from the rendered page. Reply ONLY with the requested JSON object on a single line.";
    var UserMsg = $"Dispatch QA scene {Pad}. Recorded narration: '{Narration}'. Rendered UI text from OCR: '{OcrText}'. Score how well the narration matches the rendered UI on 0.0-1.0 where 1.0 is a perfect match and 0.0 is unrelated. Then write a better dispatcher narration that matches the rendered UI in plain English without technical jargon. Reply EXACTLY: {{\"score\": 0.0-1.0, \"rewrite\": \"...\"}}";
    string EscJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    var Payload = "{\"messages\":[{\"role\":\"user\",\"content\":\"" + EscJson(UserMsg) + "\"}],\"system\":\"" + EscJson(SystemPrompt) + "\",\"max_tokens\":512}";
    var Req = new HttpRequestMessage(HttpMethod.Post, WorkerUrl.TrimEnd('/') + "/ai")
    {
        Content = new StringContent(Payload, Encoding.UTF8, "application/json"),
    };
    Req.Headers.Add("X-Wolfs-Session", SessionId);
    Req.Headers.Add("X-Wolfs-Role", "admin");
    try
    {
        var Resp = await Http.SendAsync(Req);
        var RespText = await Resp.Content.ReadAsStringAsync();
        var RespDoc = JsonDocument.Parse(RespText);
        var Text = RespDoc.RootElement.TryGetProperty("text", out var Tp) ? Tp.GetString() ?? "" : "";
        var Cleaned = Regex.Replace(Text, "```(?:json)?", "", RegexOptions.IgnoreCase).Trim();
        var JsonStart = Cleaned.IndexOf('{');
        var JsonEnd = Cleaned.LastIndexOf('}');
        var LlmScore = "?";
        var Rewrite = Text;
        if (JsonStart >= 0 && JsonEnd > JsonStart)
        {
            try
            {
                var ParsedDoc = JsonDocument.Parse(Cleaned.Substring(JsonStart, JsonEnd - JsonStart + 1));
                if (ParsedDoc.RootElement.TryGetProperty("score", out var Sp)) LlmScore = Sp.GetDouble().ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                if (ParsedDoc.RootElement.TryGetProperty("rewrite", out var Rp)) Rewrite = Rp.GetString() ?? Text;
            }
            catch { }
        }
        var NarrEsc = Narration.Replace("|", "\\|");
        var RewEsc = Rewrite.Replace("|", "\\|").Replace("\n", " ");
        if (RewEsc.Length > 200) RewEsc = RewEsc[..200] + "...";
        Md.AppendLine($"| {Pad} | {Score:F2} | {LlmScore} | {NarrEsc} | {RewEsc} |");
        Reviewed++;
        Console.WriteLine($"  {Pad} llm={LlmScore} (j={Score:F2})");
    }
    catch (Exception E)
    {
        Md.AppendLine($"| {Pad} | {Score:F2} | error | {Narration.Replace("|", "\\|")} | {E.Message.Replace("|", "\\|")} |");
        Errors++;
        Console.WriteLine($"  {Pad} ERR {E.Message}");
    }
}

Md.AppendLine();
Md.AppendLine($"_Reviewed {Reviewed} scenes, {Errors} errors. Threshold = {Threshold:F2}._");

var OutDir = Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(OutDir)) Directory.CreateDirectory(OutDir);
await File.WriteAllTextAsync(OutputPath, Md.ToString());
return 0;
