#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != '\u0022') continue;
        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var Package = Get("Package")!;
var Psi = new ProcessStartInfo("python") { RedirectStandardOutput = true, RedirectStandardError = true };
Psi.ArgumentList.Add("-m");
Psi.ArgumentList.Add("pip");
Psi.ArgumentList.Add("install");
Psi.ArgumentList.Add("--upgrade");
Psi.ArgumentList.Add(Package);
using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync();
var Err = await P.StandardError.ReadToEndAsync();
await P.WaitForExitAsync();
await Console.Error.WriteLineAsync("pip-upgrade " + Package + ": exit=" + P.ExitCode);
await Console.Error.WriteLineAsync("out: " + (Out.Length > 800 ? Out[(Out.Length - 800)..] : Out));
if (!string.IsNullOrEmpty(Err)) await Console.Error.WriteLineAsync("err: " + (Err.Length > 600 ? Err[(Err.Length - 600)..] : Err));
return P.ExitCode;
