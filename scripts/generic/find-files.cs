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

var root = Get("Root", ".");
var glob = Get("Glob", "*.*");
var max = int.Parse(Get("Max", "200"));
if (!Directory.Exists(root)) return 2;

var count = 0;
foreach (var file in Directory.GetFiles(root, glob, SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
{
    var rel = Path.GetRelativePath(root, file);
    if (rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .Any(x => string.Equals(x, "bin", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(x, "obj", StringComparison.OrdinalIgnoreCase)))
    {
        continue;
    }
    Console.WriteLine(rel);
    if (++count >= max) return 0;
}
return 0;
