#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name, string fallback = "")
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : fallback;
}

var path = Get("Path");
var max = int.Parse(Get("Max", "3"));
if (!File.Exists(path)) return 2;
using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path));
var i = 0;
foreach (var item in doc.RootElement.EnumerateArray())
{
    Console.WriteLine("item=" + i);
    foreach (var p in item.EnumerateObject())
    {
        Console.WriteLine(p.Name + "=" + p.Value.ToString());
    }
    if (++i >= max) break;
}
return 0;
