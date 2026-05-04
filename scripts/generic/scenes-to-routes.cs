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

var scenes = Get("ScenesPath");
var output = Get("OutputPath");
if (!File.Exists(scenes) || string.IsNullOrWhiteSpace(output)) return 2;
using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(scenes));
var urls = new List<string>();
foreach (var scene in doc.RootElement.EnumerateArray())
{
    if (scene.TryGetProperty("target", out var t) || scene.TryGetProperty("url", out t))
    {
        var url = t.GetString();
        if (!string.IsNullOrWhiteSpace(url)) urls.Add(url);
    }
}
var dir = Path.GetDirectoryName(output);
if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
var lines = new List<string> { "[" };
for (var i = 0; i < urls.Count; i++)
{
    var esc = urls[i].Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    lines.Add("  \"" + esc + "\"" + (i + 1 < urls.Count ? "," : ""));
}
lines.Add("]");
await File.WriteAllLinesAsync(output, lines);
Console.WriteLine("routes=" + urls.Count);
Console.WriteLine("output=" + output);
return 0;
