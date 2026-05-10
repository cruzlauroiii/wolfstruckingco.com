#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

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

var Query = Get("Query")!;
var OutputFile = Get("OutputFile")!;
var MaxResults = int.Parse(Get("MaxResults") ?? "10", CultureInfo.InvariantCulture);

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
Http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
var Url = "https://html.duckduckgo.com/html/?q=" + Uri.EscapeDataString(Query);
var Html = await Http.GetStringAsync(new Uri(Url));

var ResultRx = new Regex("<a[^>]*class=\u0022result__a\u0022[^>]*href=\u0022(?<u>[^\u0022]+)\u0022[^>]*>(?<t>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
var SnippetRx = new Regex("<a[^>]*class=\u0022result__snippet\u0022[^>]*>(?<s>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
var Sb = new StringBuilder();
Sb.Append("# Web search: ").AppendLine(Query);
Sb.AppendLine();
var Matches = ResultRx.Matches(Html);
var Snippets = SnippetRx.Matches(Html);
var Count = Math.Min(MaxResults, Matches.Count);
for (var I = 0; I < Count; I++)
{
    var U = Matches[I].Groups["u"].Value;
    var T = Regex.Replace(Matches[I].Groups["t"].Value, "<[^>]+>", string.Empty).Trim();
    var S = I < Snippets.Count ? Regex.Replace(Snippets[I].Groups["s"].Value, "<[^>]+>", string.Empty).Trim() : string.Empty;
    if (U.StartsWith("//duckduckgo.com/l/?uddg=", StringComparison.Ordinal))
    {
        var Eq = U.IndexOf("uddg=", StringComparison.Ordinal);
        var Amp = U.IndexOf('&', Eq);
        var Enc = Amp > 0 ? U[(Eq + 5)..Amp] : U[(Eq + 5)..];
        U = Uri.UnescapeDataString(Enc);
    }
    Sb.Append("## ").Append((I + 1).ToString(CultureInfo.InvariantCulture)).Append(". ").AppendLine(T);
    Sb.AppendLine(U);
    if (!string.IsNullOrEmpty(S)) Sb.AppendLine().AppendLine(S);
    Sb.AppendLine();
}
await File.WriteAllTextAsync(OutputFile, Sb.ToString());
return 0;
