#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;

static Process Spawn(string Generic, string Config, string Wd)
{
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = false, RedirectStandardError = false, WorkingDirectory = Wd, CreateNoWindow = false };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(Generic);
    Psi.ArgumentList.Add(Config);
    return Process.Start(Psi)!;
}

static async Task<(int ExitCode, string Stdout, string Stderr)> Run(string Generic, string Config, string Wd, TimeSpan Timeout)
{
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Wd };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(Generic);
    Psi.ArgumentList.Add(Config);
    using var P = Process.Start(Psi)!;
    var SoTask = P.StandardOutput.ReadToEndAsync();
    var SeTask = P.StandardError.ReadToEndAsync();
    using var Cts = new CancellationTokenSource(Timeout);
    try { await P.WaitForExitAsync(Cts.Token); }
    catch (OperationCanceledException) { try { P.Kill(true); } catch { } return (-1, await SoTask, await SeTask + "\nTIMEOUT"); }
    return (P.ExitCode, await SoTask, await SeTask);
}

var Wd = @"C:\repo\public\wolfstruckingco.com";
var Server = Spawn(@"main\scripts\generic\tunnel-server.cs", @"main\scripts\specific\tunnel-server-config.cs", Wd);
await Console.Out.WriteLineAsync("server pid=" + Server.Id.ToString(CultureInfo.InvariantCulture));
await Task.Delay(5000);

var Tester = Spawn(@"main\scripts\generic\tunnel-client.cs", @"main\scripts\specific\tunnel-client-tester-config.cs", Wd);
await Console.Out.WriteLineAsync("tester pid=" + Tester.Id.ToString(CultureInfo.InvariantCulture));
await Task.Delay(8000);
var Developer = Spawn(@"main\scripts\generic\tunnel-client.cs", @"main\scripts\specific\tunnel-client-developer-config.cs", Wd);
await Console.Out.WriteLineAsync("developer pid=" + Developer.Id.ToString(CultureInfo.InvariantCulture));
await Task.Delay(5000);

var Ok = true;
var TesterRun = await Run(@"main\scripts\generic\pub-exec.cs", @"main\scripts\specific\pub-exec-echo-tester-config.cs", Wd, TimeSpan.FromSeconds(30));
await Console.Out.WriteLineAsync("[TESTER] exit=" + TesterRun.ExitCode.ToString(CultureInfo.InvariantCulture) + " stdout=" + TesterRun.Stdout.Trim());
if (TesterRun.ExitCode != 0 || !TesterRun.Stdout.Contains("hello from tester", StringComparison.Ordinal)) { Ok = false; await Console.Error.WriteLineAsync("[TESTER] FAIL stderr=" + TesterRun.Stderr); }

var DevRun = await Run(@"main\scripts\generic\pub-exec.cs", @"main\scripts\specific\pub-exec-echo-developer-config.cs", Wd, TimeSpan.FromSeconds(30));
await Console.Out.WriteLineAsync("[DEVELOPER] exit=" + DevRun.ExitCode.ToString(CultureInfo.InvariantCulture) + " stdout=" + DevRun.Stdout.Trim());
if (DevRun.ExitCode != 0 || !DevRun.Stdout.Contains("hello from developer", StringComparison.Ordinal)) { Ok = false; await Console.Error.WriteLineAsync("[DEVELOPER] FAIL stderr=" + DevRun.Stderr); }

var DotnetRun = await Run(@"main\scripts\generic\pub-exec.cs", @"main\scripts\specific\pub-exec-dotnet-run-tester-config.cs", Wd, TimeSpan.FromSeconds(120));
await Console.Out.WriteLineAsync("[DOTNET_RUN] exit=" + DotnetRun.ExitCode.ToString(CultureInfo.InvariantCulture) + " stdout=" + DotnetRun.Stdout.Trim());
if (DotnetRun.ExitCode != 0 || !DotnetRun.Stdout.Contains("count=", StringComparison.Ordinal)) { Ok = false; await Console.Error.WriteLineAsync("[DOTNET_RUN] FAIL stderr=" + DotnetRun.Stderr); }

try { Tester.Kill(true); } catch { }
try { Developer.Kill(true); } catch { }
try { Server.Kill(true); } catch { }
Tester.Dispose();
Developer.Dispose();
Server.Dispose();

await Console.Out.WriteLineAsync(Ok ? "OK: tunnel pub/sub end-to-end works" : "FAIL: tunnel pub/sub broken");
return Ok ? 0 : 1;
