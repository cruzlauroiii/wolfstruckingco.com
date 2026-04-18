using System.Net;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/web-search.cs scripts/<web-search-X>-config.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var SpecBody = await File.ReadAllTextAsync(SpecPath);
var Strs = WebSearchPatterns.ConstString().Matches(SpecBody)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);

if (!Strs.TryGetValue("Query", out var Query)) { await Console.Error.WriteLineAsync("specific must declare const string Query"); return 3; }
var MaxResults = Strs.TryGetValue("MaxResults", out var M) && int.TryParse(M, System.Globalization.CultureInfo.InvariantCulture, out var Mn) ? Mn : 8;

using var Handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate, CheckCertificateRevocationList = true };
using var Http = new HttpClient(Handler);
Http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36");
Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
Http.Timeout = TimeSpan.FromSeconds(20);

var Url = new Uri($"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(Query)}");
var Html = await Http.GetStringAsync(Url);

var Hits = WebSearchPatterns.Result().Matches(Html).Select(Mh => (Mh.Groups["href"].Value, Mh.Groups["title"].Value, Mh.Groups["snippet"].Value));
var Count = 0;
foreach (var (HrefRaw, TitleRaw, SnippetRaw) in Hits)
{
    if (Count >= MaxResults) { break; }
    var Href = WebUtility.HtmlDecode(HrefRaw);
    var Title = WebUtility.HtmlDecode(WebSearchPatterns.StripTags().Replace(TitleRaw, string.Empty)).Trim();
    var Snippet = WebUtility.HtmlDecode(WebSearchPatterns.StripTags().Replace(SnippetRaw, string.Empty)).Trim();
    if (Href.StartsWith("//duckduckgo.com/l/?uddg=", StringComparison.Ordinal))
    {
        var Q = Href.IndexOf("uddg=", StringComparison.Ordinal) + 5;
#pragma warning disable MA0074
        var End = Href.IndexOf('&', Q);
#pragma warning restore MA0074
        Href = Uri.UnescapeDataString(End < 0 ? Href[Q..] : Href[Q..End]);
    }
    await Console.Out.WriteLineAsync($"{(Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}. {Title}");
    await Console.Out.WriteLineAsync($"   {Href}");
    await Console.Out.WriteLineAsync($"   {Snippet}");
    await Console.Out.WriteLineAsync(string.Empty);
    Count++;
}

if (Count == 0) { await Console.Error.WriteLineAsync($"no results for: {Query}"); return 4; }
return 0;

namespace Scripts
{
    internal static partial class WebSearchPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex("""<a\s+rel="nofollow"\s+class="result__a"\s+href="(?<href>[^"]+)"[^>]*>(?<title>.+?)</a>.+?<a\s+class="result__snippet"[^>]*>(?<snippet>.+?)</a>""", RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
        internal static partial Regex Result();

        [GeneratedRegex("<[^>]+>", RegexOptions.ExplicitCapture)]
        internal static partial Regex StripTags();
    }
}
