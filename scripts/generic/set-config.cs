#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;

var SpecLines = await File.ReadAllLinesAsync(SpecPath);
string? ReadConst(string Name)
{
    foreach (var Line in SpecLines)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        if (After.StartsWith("@", StringComparison.Ordinal)) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}

var TargetPath = ReadConst("TargetConfig");
if (TargetPath is null || !File.Exists(TargetPath)) return 3;

var Settings = new Dictionary<string, string>();
var SetRe = new Regex(@"const\s+string\s+Set_(?<n>\w+)\s*=\s*@?""(?<v>.*)""\s*;\s*$");
foreach (var Line in SpecLines)
{
    var M = SetRe.Match(Line);
    if (M.Success) Settings[M.Groups["n"].Value] = M.Groups["v"].Value;
}
if (Settings.Count == 0) return 4;

var TargetText = await File.ReadAllTextAsync(TargetPath);
var TargetLines = TargetText.Split('\n');
for (var I = 0; I < TargetLines.Length; I++)
{
    foreach (var (Name, Val) in Settings)
    {
        var LineRe = new Regex(@"^(?<indent>\s*)public\s+const\s+string\s+" + Regex.Escape(Name) + @"\s*=\s*@?""[^""]*""\s*;\s*\r?$");
        var M = LineRe.Match(TargetLines[I]);
        if (M.Success)
        {
            TargetLines[I] = $"{M.Groups["indent"].Value}public const string {Name} = @\"{Val}\";" + (TargetLines[I].EndsWith("\r") ? "\r" : "");
        }
    }
}
await File.WriteAllTextAsync(TargetPath, string.Join("\n", TargetLines));
return 0;
