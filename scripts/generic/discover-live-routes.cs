#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

string? Origin = null;
string? OutputPath = null;
int MaxDepth = 3;
foreach (var Line in await File.ReadAllLinesAsync(SpecPath))
{
    var Idx = Line.IndexOf("const string ", StringComparison.Ordinal);
    if (Idx >= 0)
    {
        var After = Line.Substring(Idx + 13);
        var Eq = After.IndexOf(" = ", StringComparison.Ordinal);
        if (Eq < 0) continue;
        var Name = After.Substring(0, Eq).Trim();
        var Rhs = After.Substring(Eq + 3).TrimStart();
        if (Rhs.StartsWith("@", StringComparison.Ordinal)) Rhs = Rhs.Substring(1);
        if (!Rhs.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = Rhs.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        var Value = Rhs.Substring(1, End - 1);
        if (Name == "Origin") Origin = Value;
        else if (Name == "OutputPath") OutputPath = Value;
    }
    var IntIdx = Line.IndexOf("const int ", StringComparison.Ordinal);
    if (IntIdx >= 0)
    {
        var After = Line.Substring(IntIdx + 10);
        var Eq = After.IndexOf(" = ", StringComparison.Ordinal);
        if (Eq < 0) continue;
        var Name = After.Substring(0, Eq).Trim();
        var Rhs = After.Substring(Eq + 3).TrimStart();
        var Semi = Rhs.IndexOf(";", StringComparison.Ordinal);
        if (Semi < 0) continue;
        if (int.TryParse(Rhs.Substring(0, Semi), out var V) && Name == "MaxDepth") MaxDepth = V;
    }
}
if (Origin is null || OutputPath is null) return 3;

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
Http.DefaultRequestHeaders.UserAgent.ParseAdd("WolfsWalkthrough/1.0");

var Visited = new HashSet<string>(StringComparer.Ordinal);
var Order = new List<string>();
var Queue = new Queue<(string Url, int Depth)>();
Queue.Enqueue((Origin + "/", 0));

var HrefRe = new Regex("href=\"([^\"]+)\"", RegexOptions.IgnoreCase);

while (Queue.Count > 0)
{
    var (Url, Depth) = Queue.Dequeue();
    if (Visited.Contains(Url)) continue;
    Visited.Add(Url);
    string Body;
    try { Body = await Http.GetStringAsync(Url); }
    catch { continue; }
    Order.Add(Url);
    if (Depth >= MaxDepth) continue;
    foreach (Match M in HrefRe.Matches(Body))
    {
        var Href = M.Groups[1].Value;
        if (Href.StartsWith("#", StringComparison.Ordinal)) continue;
        if (Href.StartsWith("mailto:", StringComparison.Ordinal)) continue;
        if (Href.StartsWith("tel:", StringComparison.Ordinal)) continue;
        if (Href.StartsWith("javascript:", StringComparison.Ordinal)) continue;
        string Abs;
        if (Href.StartsWith("http", StringComparison.Ordinal))
        {
            if (!Href.StartsWith(Origin, StringComparison.Ordinal)) continue;
            Abs = Href.Split('#')[0].Split('?')[0];
        }
        else if (Href.StartsWith("/", StringComparison.Ordinal))
        {
            Abs = Origin + Href.Split('#')[0].Split('?')[0];
        }
        else
        {
            try { Abs = new Uri(new Uri(Url), Href).ToString().Split('#')[0].Split('?')[0]; }
            catch { continue; }
            if (!Abs.StartsWith(Origin, StringComparison.Ordinal)) continue;
        }
        if (Abs.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || Abs.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || Abs.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || Abs.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || Abs.EndsWith(".css", StringComparison.OrdinalIgnoreCase) || Abs.EndsWith(".js", StringComparison.OrdinalIgnoreCase) || Abs.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) continue;
        if (!Abs.EndsWith("/", StringComparison.Ordinal) && !Abs.Substring(Origin.Length).Contains(".", StringComparison.Ordinal)) Abs = Abs + "/";
        if (Visited.Contains(Abs)) continue;
        Queue.Enqueue((Abs, Depth + 1));
    }
}

var Sb = new StringBuilder();
Sb.Append("[\n");
for (var I = 0; I < Order.Count; I++)
{
    Sb.Append("  \"");
    Sb.Append(Order[I].Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal));
    Sb.Append("\"");
    if (I < Order.Count - 1) Sb.Append(",");
    Sb.Append("\n");
}
Sb.Append("]\n");
var OutDir = System.IO.Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(OutDir) && !Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);
await File.WriteAllTextAsync(OutputPath, Sb.ToString());
return 0;
