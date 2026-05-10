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

var DevtunnelExe = Get("DevtunnelExe") ?? "devtunnel";
var TunnelName = Get("TunnelName") ?? "wolfs-execution";
var Port = Get("Port") ?? "4444";

static async Task<(int, string, string)> Run(string Exe, params string[] Args)
{
    var Psi = new ProcessStartInfo(Exe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var A in Args) Psi.ArgumentList.Add(A);
    using var P = Process.Start(Psi)!;
    var O = await P.StandardOutput.ReadToEndAsync();
    var E = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    return (P.ExitCode, O, E);
}

var D = await Run(DevtunnelExe, "delete-all", "-f");
await Console.Out.WriteAsync(D.Item2);
if (!string.IsNullOrEmpty(D.Item3)) await Console.Error.WriteAsync(D.Item3);
var C = await Run(DevtunnelExe, "create", TunnelName);
await Console.Out.WriteAsync(C.Item2);
if (!string.IsNullOrEmpty(C.Item3)) await Console.Error.WriteAsync(C.Item3);
var P1 = await Run(DevtunnelExe, "port", "create", TunnelName, "-p", Port, "--protocol", "http");
await Console.Out.WriteAsync(P1.Item2);
if (!string.IsNullOrEmpty(P1.Item3)) await Console.Error.WriteAsync(P1.Item3);
var A = await Run(DevtunnelExe, "access", "create", TunnelName, "-p", Port, "--anonymous");
await Console.Out.WriteAsync(A.Item2);
if (!string.IsNullOrEmpty(A.Item3)) await Console.Error.WriteAsync(A.Item3);
return 0;
