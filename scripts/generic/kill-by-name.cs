#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);
var Quote = (char)0x22;

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != Quote) continue;
        var Term = string.Concat(Quote.ToString(CultureInfo.InvariantCulture), ";");
        var End = Tail.LastIndexOf(Term, StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var ProcessName = Get("ProcessName")!;
var Killed = 0;
foreach (var P in Process.GetProcessesByName(ProcessName))
{
    try { P.Kill(entireProcessTree: true); P.WaitForExit(5000); Killed++; }
    catch (Exception E) { await Console.Error.WriteLineAsync("kill skip pid=" + P.Id.ToString(CultureInfo.InvariantCulture) + " " + E.Message); }
}
await Console.Error.WriteLineAsync("kill-by-name: name=" + ProcessName + " killed=" + Killed.ToString(CultureInfo.InvariantCulture));
return 0;
