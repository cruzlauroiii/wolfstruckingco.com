#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var specPath = args[0];
if (!File.Exists(specPath)) return 2;
var spec = await File.ReadAllTextAsync(specPath);

string Get(string name)
{
    var match = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return match.Success ? match.Groups["v"].Value : "";
}

var wrapper = Get("WrapperPath");
var prefix = Get("ChunkPrefix");
if (string.IsNullOrWhiteSpace(wrapper) || !File.Exists(wrapper)) return 3;

var dir = Path.GetDirectoryName(wrapper)!;
var parts = Directory.GetFiles(dir, prefix + "-*.inc").OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
if (parts.Length == 0) return 4;

var merged = new List<string>();
foreach (var part in parts)
{
    merged.AddRange(await File.ReadAllLinesAsync(part));
}

await File.WriteAllLinesAsync(wrapper, merged);
Console.WriteLine($"{Path.GetFileName(wrapper)} merged from {parts.Length} fragments");
return 0;
