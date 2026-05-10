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

var Wd = Get("Wd")!;
var ClientConfig = Get("ClientConfig")!;
var TimeoutSeconds = int.Parse(Get("TimeoutSeconds") ?? "15", System.Globalization.CultureInfo.InvariantCulture);

var Psi = new ProcessStartInfo("dotnet")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = Wd,
    CreateNoWindow = true,
};
Psi.ArgumentList.Add("run");
Psi.ArgumentList.Add(@"main\scripts\generic\tunnel-client.cs");
Psi.ArgumentList.Add(ClientConfig);
using var Proc = Process.Start(Psi)!;
var OutTask = Task.Run(async () => { var sb = new System.Text.StringBuilder(); var rd = Proc.StandardOutput; while (!Proc.HasExited && sb.Length < 4000) { var l = await rd.ReadLineAsync(); if (l == null) break; sb.AppendLine("OUT: " + l); } return sb.ToString(); });
var ErrTask = Task.Run(async () => { var sb = new System.Text.StringBuilder(); var rd = Proc.StandardError; while (!Proc.HasExited && sb.Length < 4000) { var l = await rd.ReadLineAsync(); if (l == null) break; sb.AppendLine("ERR: " + l); } return sb.ToString(); });
await Task.WhenAny(Proc.WaitForExitAsync(), Task.Delay(TimeSpan.FromSeconds(TimeoutSeconds)));
if (!Proc.HasExited) { try { Proc.Kill(true); } catch { } }
var OutText = await OutTask;
var ErrText = await ErrTask;
await Console.Error.WriteLineAsync("probe-tunnel-client exited=" + Proc.HasExited + " exitCode=" + Proc.ExitCode);
await Console.Error.WriteLineAsync(OutText.Length > 2000 ? OutText[..2000] : OutText);
await Console.Error.WriteLineAsync(ErrText.Length > 2000 ? ErrText[..2000] : ErrText);
return 0;
