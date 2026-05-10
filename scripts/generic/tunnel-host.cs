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

static async Task<int> Run(string Cmd, params string[] Args)
{
    var Psi = new ProcessStartInfo(Cmd) { UseShellExecute = false, RedirectStandardOutput = false, RedirectStandardError = false };
    foreach (var A in Args) Psi.ArgumentList.Add(A);
    using var P = Process.Start(Psi)!;
    await P.WaitForExitAsync();
    return P.ExitCode;
}

static async Task<int> RunSilent(string Cmd, params string[] Args)
{
    var Psi = new ProcessStartInfo(Cmd) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var A in Args) Psi.ArgumentList.Add(A);
    using var P = Process.Start(Psi)!;
    _ = await P.StandardOutput.ReadToEndAsync();
    _ = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    return P.ExitCode;
}

_ = await RunSilent(DevtunnelExe, "create", TunnelName);
_ = await RunSilent(DevtunnelExe, "port", "create", TunnelName, "-p", Port, "--protocol", "http");
_ = await RunSilent(DevtunnelExe, "access", "create", TunnelName, "-p", Port, "--anonymous");
await Console.Out.WriteLineAsync("tunnel-host: about to run `devtunnel host -p " + Port + "` (long-running). Open a new pane to keep this alive while you use the tunnel; Ctrl+C to stop.");
return await Run(DevtunnelExe, "host", "-p", Port);
