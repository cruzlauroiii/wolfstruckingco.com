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

var Root = Get("Root") ?? "";
var Pattern = Get("Pattern") ?? "*";
var OutputPath = Get("OutputPath") ?? "";
if (string.IsNullOrEmpty(Root) || !Directory.Exists(Root)) return 3;

var Files = Directory.GetFiles(Root, Pattern, SearchOption.AllDirectories);
var Output = string.Join("\n", Files);
if (!string.IsNullOrEmpty(OutputPath))
{
    var Dir = Path.GetDirectoryName(OutputPath);
    if (!string.IsNullOrEmpty(Dir)) Directory.CreateDirectory(Dir);
    await File.WriteAllTextAsync(OutputPath, Output);
}
else
{
    Console.WriteLine(Output);
}
return 0;
