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

static async Task<(int, string, string)> Run(string Generic, string Config, string Wd, TimeSpan Timeout)
{
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Wd };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(Generic);
    Psi.ArgumentList.Add(Config);
    using var P = Process.Start(Psi)!;
    var SoT = P.StandardOutput.ReadToEndAsync();
    var SeT = P.StandardError.ReadToEndAsync();
    using var Cts = new CancellationTokenSource(Timeout);
    try { await P.WaitForExitAsync(Cts.Token); }
    catch (OperationCanceledException) { try { P.Kill(true); } catch { } return (-1, await SoT, await SeT + "\nTIMEOUT"); }
    return (P.ExitCode, await SoT, await SeT);
}

var Wd = @"C:\repo\public\wolfstruckingco.com";
var Server = Spawn(@"main\scripts\generic\tunnel-server.cs", @"main\scripts\specific\tunnel-server-config.cs", Wd);
await Console.Out.WriteLineAsync("server pid=" + Server.Id.ToString(CultureInfo.InvariantCulture));
await Task.Delay(5000);

var TunnelHost = Spawn(@"main\scripts\generic\tunnel-host.cs", @"main\scripts\specific\tunnel-host-config.cs", Wd);
await Console.Out.WriteLineAsync("tunnel-host pid=" + TunnelHost.Id.ToString(CultureInfo.InvariantCulture));
await Task.Delay(15000);

var Tester = Spawn(@"main\scripts\generic\tunnel-client.cs", @"main\scripts\specific\tunnel-client-tester-cloud-config.cs", Wd);
await Console.Out.WriteLineAsync("tester(cloud) pid=" + Tester.Id.ToString(CultureInfo.InvariantCulture));
await Task.Delay(10000);
var Developer = Spawn(@"main\scripts\generic\tunnel-client.cs", @"main\scripts\specific\tunnel-client-developer-cloud-config.cs", Wd);
await Console.Out.WriteLineAsync("developer(cloud) pid=" + Developer.Id.ToString(CultureInfo.InvariantCulture));
await Task.Delay(10000);

var Ok = true;

async Task Step(string Label, string ConfigPath, string Expected)
{
    var R = await Run(@"main\scripts\generic\pub-exec.cs", ConfigPath, Wd, TimeSpan.FromSeconds(90));
    await Console.Out.WriteLineAsync("[" + Label + "] exit=" + R.Item1.ToString(CultureInfo.InvariantCulture) + " stdout=" + R.Item2.Trim());
    if (R.Item1 != 0 || !R.Item2.Contains(Expected, StringComparison.Ordinal))
    {
        Ok = false;
        await Console.Error.WriteLineAsync("[" + Label + "] FAIL stderr=" + R.Item3);
    }
}

await Step("ECHO_TESTER", @"main\scripts\specific\pub-exec-echo-tester-cloud-config.cs", "hello via cloud tunnel");
await Step("ECHO_DEVELOPER", @"main\scripts\specific\pub-exec-echo-developer-cloud-config.cs", "hello dev via cloud tunnel");
await Step("DOTNET_RUN_TESTER", @"main\scripts\specific\pub-exec-dotnet-run-tester-cloud-config.cs", "count=");
await Step("DOTNET_RUN_DEVELOPER", @"main\scripts\specific\pub-exec-dotnet-run-developer-cloud-config.cs", "count=");

try { Tester.Kill(true); } catch { }
try { Developer.Kill(true); } catch { }
try { TunnelHost.Kill(true); } catch { }
try { Server.Kill(true); } catch { }
Tester.Dispose(); Developer.Dispose(); TunnelHost.Dispose(); Server.Dispose();

await Console.Out.WriteLineAsync(Ok ? "OK: cloud tunnel pub/sub end-to-end works for tester+developer, echo+dotnet_run" : "FAIL: cloud tunnel broken");
return Ok ? 0 : 1;
