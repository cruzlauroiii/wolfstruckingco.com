#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var specPath = args[0];
if (!File.Exists(specPath)) return 2;
var spec = await File.ReadAllTextAsync(specPath);

string Get(string name, string fallback = "")
{
    var match = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return match.Success ? match.Groups["v"].Value : fallback;
}

var wrapper = Get("WrapperPath");
var prefix = Get("ChunkPrefix");
if (string.IsNullOrWhiteSpace(wrapper) || !File.Exists(wrapper)) return 3;
if (string.IsNullOrWhiteSpace(prefix)) return 4;

var dir = Path.GetDirectoryName(wrapper)!;
var chunkPaths = Directory.GetFiles(dir, prefix + "-*.cs").OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
if (chunkPaths.Length == 0) return 5;

var lines = new List<string> { "#:property ExperimentalFileBasedProgramEnableTransitiveDirectives=true" };
foreach (var oldPath in chunkPaths)
{
    var inc = Path.ChangeExtension(oldPath, ".inc");
    if (File.Exists(inc)) File.Delete(inc);
    File.Move(oldPath, inc);
    lines.Add("#:include " + Path.GetFileName(inc));
}

await File.WriteAllLinesAsync(wrapper, lines);
Console.WriteLine($"{Path.GetFileName(wrapper)} now includes {chunkPaths.Length} text fragments");
return 0;
