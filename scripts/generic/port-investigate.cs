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

var FilterRegex = Get("FilterRegex") ?? ".*";
var Rx = new Regex(FilterRegex, RegexOptions.IgnoreCase);

var Psi = new ProcessStartInfo("netstat") { RedirectStandardOutput = true };
Psi.ArgumentList.Add("-ano");
using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync();
await P.WaitForExitAsync();

var ProcMap = new Dictionary<int, string>();
foreach (var Pr in Process.GetProcesses())
{
    try { ProcMap[Pr.Id] = Pr.ProcessName; } catch { }
}

foreach (var Line in Out.Split('\n'))
{
    var T = Line.Trim();
    if (!T.StartsWith("TCP", StringComparison.Ordinal)) continue;
    if (!T.Contains("LISTENING")) continue;
    var Parts = Regex.Split(T, "\\s+");
    if (Parts.Length < 5) continue;
    var Local = Parts[1];
    var Pid = Parts[Parts.Length - 1];
    if (!int.TryParse(Pid, out var PidI)) continue;
    var Name = ProcMap.TryGetValue(PidI, out var N) ? N : "?";
    var Combined = $"{Local}\t{Name}(pid={Pid})";
    if (Rx.IsMatch(Combined)) Console.WriteLine(Combined);
}

return 0;
