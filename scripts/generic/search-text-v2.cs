#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string? Get(string Name)
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

var SourcePath = Get("SourcePath") ?? "";
var Pattern = Get("Pattern") ?? "";
if (string.IsNullOrEmpty(SourcePath) || !File.Exists(SourcePath) || string.IsNullOrEmpty(Pattern)) return 3;
var Lines = await File.ReadAllLinesAsync(SourcePath);
for (int I = 0; I < Lines.Length; I++)
{
    if (Lines[I].Contains(Pattern, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"{I + 1}: {Lines[I]}");
    }
}
return 0;
