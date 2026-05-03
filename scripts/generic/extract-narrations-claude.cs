#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

string? RoutesPath = null;
string? OutputPath = null;
string? AiUrl = null;
int MaxTokens = 80;
foreach (var Line in await File.ReadAllLinesAsync(SpecPath))
{
    var SIdx = Line.IndexOf("const string ", StringComparison.Ordinal);
    if (SIdx >= 0)
    {
        var After = Line.Substring(SIdx + 13);
        var Eq = After.IndexOf(" = ", StringComparison.Ordinal);
        if (Eq < 0) continue;
        var Name = After.Substring(0, Eq).Trim();
        var Rhs = After.Substring(Eq + 3).TrimStart();
        if (Rhs.StartsWith("@", StringComparison.Ordinal)) Rhs = Rhs.Substring(1);
        if (!Rhs.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = Rhs.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        var Value = Rhs.Substring(1, End - 1);
        if (Name == "RoutesPath") RoutesPath = Value;
        else if (Name == "OutputPath") OutputPath = Value;
        else if (Name == "AiUrl") AiUrl = Value;
    }
    var IIdx = Line.IndexOf("const int ", StringComparison.Ordinal);
    if (IIdx >= 0)
    {
        var After = Line.Substring(IIdx + 10);
        var Eq = After.IndexOf(" = ", StringComparison.Ordinal);
        if (Eq < 0) continue;
        var Name = After.Substring(0, Eq).Trim();
        var Rhs = After.Substring(Eq + 3).TrimStart();
        var Semi = Rhs.IndexOf(";", StringComparison.Ordinal);
        if (Semi < 0) continue;
        if (int.TryParse(Rhs.Substring(0, Semi), out var V) && Name == "MaxTokens") MaxTokens = V;
    }
}
if (RoutesPath is null || OutputPath is null || AiUrl is null) return 3;
if (!File.Exists(RoutesPath)) return 4;

var Routes = JsonDocument.Parse(await File.ReadAllTextAsync(RoutesPath)).RootElement.EnumerateArray().Select(E => E.GetString() ?? "").Where(S => !string.IsNullOrEmpty(S)).ToArray();
using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
Http.DefaultRequestHeaders.UserAgent.ParseAdd("WolfsWalkthrough/1.0");
var AnthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
if (!string.IsNullOrEmpty(AnthropicKey)) Http.DefaultRequestHeaders.Add("X-Anthropic-Key", AnthropicKey);

var TagRe = new Regex("<[^>]+>");
var WhitespaceRe = new Regex("\\s+");
var ScriptRe = new Regex("<script[\\s\\S]*?</script>", RegexOptions.IgnoreCase);
var StyleRe = new Regex("<style[\\s\\S]*?</style>", RegexOptions.IgnoreCase);

var Narrations = new List<string>();
for (var I = 0; I < Routes.Length; I++)
{
    var Url = Routes[I];
    string Html;
    try { Html = await Http.GetStringAsync(Url); }
    catch (Exception E) { await Console.Error.WriteLineAsync($"fetch failed {Url}: {E.Message}"); return 5; }
    var Stripped = ScriptRe.Replace(Html, " ");
    Stripped = StyleRe.Replace(Stripped, " ");
    Stripped = TagRe.Replace(Stripped, " ");
    Stripped = System.Net.WebUtility.HtmlDecode(Stripped);
    Stripped = WhitespaceRe.Replace(Stripped, " ").Trim();
    if (Stripped.Length > 2000) Stripped = Stripped.Substring(0, 2000);

    var H1Re = new Regex("<h1[^>]*>([\\s\\S]*?)</h1>", RegexOptions.IgnoreCase);
    var TitleRe = new Regex("<title[^>]*>([\\s\\S]*?)</title>", RegexOptions.IgnoreCase);
    var H1Match = H1Re.Match(Html);
    var TitleMatch = TitleRe.Match(Html);
    var FallbackText = H1Match.Success ? TagRe.Replace(H1Match.Groups[1].Value, " ").Trim() : (TitleMatch.Success ? TitleMatch.Groups[1].Value.Trim() : Url);
    FallbackText = WhitespaceRe.Replace(System.Net.WebUtility.HtmlDecode(FallbackText), " ").Trim();

    var Prompt = $"Page URL: {Url}\n\nVisible text on the page:\n{Stripped}\n\nWrite ONE short sentence (max 18 words) describing what this page shows a visitor. Plain natural English. No markdown, no asterisks, no template placeholders, no emoji. Reply with just the sentence.";
    var EscPrompt = Prompt.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal).Replace("\t", " ", StringComparison.Ordinal);
    var BodyJson = "{\"max_tokens\":" + MaxTokens + ",\"system\":\"You write one short concrete English sentence per page describing what the visitor sees. Always reply with exactly one sentence and nothing else.\",\"messages\":[{\"role\":\"user\",\"content\":\"" + EscPrompt + "\"}]}";
    string Narration = FallbackText;
    try
    {
        using var ReqContent = new StringContent(BodyJson, System.Text.Encoding.UTF8, "application/json");
        using var Resp = await Http.PostAsync(AiUrl, ReqContent);
        var Json = await Resp.Content.ReadAsStringAsync();
        if (Resp.IsSuccessStatusCode)
        {
            using var Doc = JsonDocument.Parse(Json);
            if (Doc.RootElement.TryGetProperty("content", out var ContentArr) && ContentArr.ValueKind == JsonValueKind.Array && ContentArr.GetArrayLength() > 0)
            {
                var First = ContentArr[0];
                if (First.TryGetProperty("text", out var Text)) Narration = Text.GetString()?.Trim() ?? FallbackText;
            }
            else if (Doc.RootElement.TryGetProperty("reply", out var Reply))
            {
                Narration = Reply.GetString()?.Trim() ?? FallbackText;
            }
        }
    }
    catch { }
    Narration = Narration.Replace("\n", " ", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal);
    Narration = WhitespaceRe.Replace(Narration, " ").Trim();
    if (Narration.Length > 220) Narration = Narration.Substring(0, 220);
    Narrations.Add(Narration);
}

var Sb = new StringBuilder();
Sb.Append("[\n");
for (var I = 0; I < Narrations.Count; I++)
{
    var Esc = Narrations[I].Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    Sb.Append("  {\"index\":");
    Sb.Append((I + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
    Sb.Append(",\"url\":\"");
    Sb.Append(Routes[I].Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal));
    Sb.Append("\",\"narration\":\"");
    Sb.Append(Esc);
    Sb.Append("\"}");
    if (I < Narrations.Count - 1) Sb.Append(",");
    Sb.Append("\n");
}
Sb.Append("]\n");
var OutDir = System.IO.Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(OutDir) && !Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);
await File.WriteAllTextAsync(OutputPath, Sb.ToString());
return 0;
