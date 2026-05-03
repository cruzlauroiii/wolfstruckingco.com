#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

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

var Port = Get("Port") ?? "";
if (string.IsNullOrEmpty(Port)) return 3;

var Psi = new ProcessStartInfo("netstat") { RedirectStandardOutput = true };
Psi.ArgumentList.Add("-ano");
using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync();
await P.WaitForExitAsync();

var Pids = new HashSet<int>();
foreach (var Line in Out.Split('\n'))
{
    if (!Line.Contains(":" + Port)) continue;
    var Parts = Regex.Split(Line.Trim(), "\\s+");
    if (Parts.Length < 5) continue;
    if (int.TryParse(Parts[Parts.Length - 1], out var Pid) && Pid > 4) Pids.Add(Pid);
}

foreach (var Pid in Pids)
{
    try { Process.GetProcessById(Pid).Kill(true); } catch { }
}
return 0;
