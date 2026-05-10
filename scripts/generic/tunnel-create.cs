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

var TunnelName = Get("TunnelName") ?? "wolfs-execution";
var Port = Get("Port") ?? "4444";
var DevtunnelExe = Get("DevtunnelExe") ?? "devtunnel";

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

var R1 = await Run(DevtunnelExe, "create", TunnelName);
await Console.Out.WriteAsync(R1.Item2);
await Console.Error.WriteAsync(R1.Item3);
var R2 = await Run(DevtunnelExe, "port", "create", TunnelName, "-p", Port, "--protocol", "http");
await Console.Out.WriteAsync(R2.Item2);
await Console.Error.WriteAsync(R2.Item3);
var R3 = await Run(DevtunnelExe, "access", "create", TunnelName, "-p", Port, "--anonymous");
await Console.Out.WriteAsync(R3.Item2);
await Console.Error.WriteAsync(R3.Item3);
return 0;
