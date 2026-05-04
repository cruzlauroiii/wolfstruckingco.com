#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name, string fallback = "")
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : fallback;
}

var query = Get("Query");
var max = int.Parse(Get("Max", "10"));
if (string.IsNullOrWhiteSpace(query)) return 2;

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("wolfstruckingco-script", "1.0"));
var url = "https://api.github.com/search/repositories?q=" + Uri.EscapeDataString(query)
    + "&sort=stars&order=desc&per_page=" + Math.Clamp(max, 1, 25);
using var doc = JsonDocument.Parse(await http.GetStringAsync(url));
Console.WriteLine("query=" + query);
Console.WriteLine("total_count=" + doc.RootElement.GetProperty("total_count").GetInt32());
foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
{
    Console.WriteLine(item.GetProperty("full_name").GetString());
    Console.WriteLine("stars=" + item.GetProperty("stargazers_count").GetInt32());
    Console.WriteLine("url=" + item.GetProperty("html_url").GetString());
    Console.WriteLine("description=" + item.GetProperty("description").GetString());
    Console.WriteLine();
}
return 0;
