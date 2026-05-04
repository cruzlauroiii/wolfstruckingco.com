#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Spec = await File.ReadAllTextAsync(SpecPath);

string Get(string Name, string Default = "")
{
    var Match = Regex.Match(Spec, @"const\s+string\s+" + Name + @"\s*=\s*@?""(?<v>[^""]*)""\s*;");
    return Match.Success ? Match.Groups["v"].Value : Default;
}

var Root = Get("Root");
var Patterns = Get("Patterns", "*.cs").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var MaxLines = int.Parse(Get("MaxLines", "300"));
if (string.IsNullOrWhiteSpace(Root) || !Directory.Exists(Root)) return 3;

var Files = new List<string>();
foreach (var Pattern in Patterns)
{
    Files.AddRange(Directory.GetFiles(Root, Pattern, SearchOption.AllDirectories));
}

var TooLarge = false;
foreach (var FilePath in Files.Distinct().OrderBy(P => P, StringComparer.OrdinalIgnoreCase))
{
    var Count = File.ReadLines(FilePath).Count();
    if (Count > MaxLines)
    {
        TooLarge = true;
        Console.WriteLine($"{Count}\t{Path.GetRelativePath(Root, FilePath)}");
    }
}

return TooLarge ? 10 : 0;
