#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;

var Specs = await File.ReadAllLinesAsync(SpecPath);
string? Read(string Name)
{
    foreach (var Line in Specs)
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

var Root = Read("Root");
var Pattern = Read("Pattern");
if (Root is null || Pattern is null) return 3;
if (!Directory.Exists(Root)) return 4;

var Deleted = 0;
var Errs = 0;
foreach (var P in Directory.EnumerateFiles(Root, Pattern, SearchOption.TopDirectoryOnly))
{
    try { File.Delete(P); Deleted++; }
    catch { Errs++; }
}
return (Deleted == 0 && Errs == 0) ? 5 : 0;
