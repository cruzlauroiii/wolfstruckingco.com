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

var root = Get("Root");
var pattern = Get("Pattern");
var glob = Get("Glob", "*.*");
var max = int.Parse(Get("Max", "200"));
if (!Directory.Exists(root) || string.IsNullOrWhiteSpace(pattern)) return 2;

var re = new Regex(pattern, RegexOptions.IgnoreCase);
var count = 0;
var textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".cs", ".razor", ".cshtml", ".html", ".htm", ".js", ".ts", ".css",
    ".json", ".md", ".xml", ".props", ".targets", ".yml", ".yaml"
};
static bool IsBuildPath(string file)
{
    var parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return parts.Any(x =>
        string.Equals(x, "bin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(x, "obj", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(x, ".git", StringComparison.OrdinalIgnoreCase));
}
foreach (var file in Directory.GetFiles(root, glob, SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
{
    if (IsBuildPath(file) || !textExtensions.Contains(Path.GetExtension(file))) continue;
    var rel = Path.GetRelativePath(root, file);
    string[] lines;
    try { lines = await File.ReadAllLinesAsync(file); } catch { continue; }
    for (var i = 0; i < lines.Length; i++)
    {
        if (!re.IsMatch(lines[i])) continue;
        Console.WriteLine($"{rel}:{i + 1}: {lines[i].Trim()}");
        if (++count >= max) return 0;
    }
}
return 0;
