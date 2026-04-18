#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var Specs = await File.ReadAllLinesAsync(SpecPath);
var Deleted = 0;
var Missing = 0;
foreach (var Line in Specs)
{
    var Idx = Line.IndexOf("const string Path", StringComparison.Ordinal);
    if (Idx < 0) continue;
    var After = Line.Substring(Idx);
    var Eq = After.IndexOf(" = ", StringComparison.Ordinal);
    if (Eq < 0) continue;
    var Rhs = After.Substring(Eq + 3).TrimStart();
    if (Rhs.StartsWith("@", StringComparison.Ordinal)) Rhs = Rhs.Substring(1);
    if (!Rhs.StartsWith("\"", StringComparison.Ordinal)) continue;
    var End = Rhs.LastIndexOf("\";", StringComparison.Ordinal);
    if (End < 1) continue;
    var Path = Rhs.Substring(1, End - 1);
    if (File.Exists(Path)) { File.Delete(Path); Deleted++; await Console.Error.WriteLineAsync($"deleted {Path}"); }
    else { Missing++; await Console.Error.WriteLineAsync($"missing {Path}"); }
}
await Console.Error.WriteLineAsync($"deleted {Deleted}, missing {Missing}");
return 0;
