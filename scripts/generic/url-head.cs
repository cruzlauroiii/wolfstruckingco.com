#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name, string fallback = "")
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : fallback;
}

var url = Get("Url");
if (string.IsNullOrWhiteSpace(url)) return 2;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
using var req = new HttpRequestMessage(HttpMethod.Head, url);
using var resp = await http.SendAsync(req);
Console.WriteLine("url=" + url);
Console.WriteLine("status=" + (int)resp.StatusCode);
Console.WriteLine("content-length=" + (resp.Content.Headers.ContentLength?.ToString() ?? ""));
Console.WriteLine("content-type=" + string.Join(",", resp.Content.Headers.ContentType?.ToString() ?? ""));
Console.WriteLine("last-modified=" + (resp.Content.Headers.LastModified?.ToString("O") ?? ""));
Console.WriteLine("etag=" + string.Join(",", resp.Headers.ETag?.ToString() ?? ""));
return resp.IsSuccessStatusCode ? 0 : 3;
