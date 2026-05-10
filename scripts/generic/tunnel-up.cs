#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

static Process Spawn(string Generic, string Config, string Wd)
{
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = false, RedirectStandardError = false, WorkingDirectory = Wd, CreateNoWindow = false };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(Generic);
    Psi.ArgumentList.Add(Config);
    return Process.Start(Psi)!;
}

var Wd = @"C:\repo\public\wolfstruckingco.com";
var Cdt = Spawn(@"main\scripts\generic\chrome-devtools.cs", @"main\scripts\specific\chrome-devtools-serve-config.cs", Wd);
await Console.Out.WriteLineAsync("chrome-devtools serve pid=" + Cdt.Id);
await Task.Delay(10000);
var Server = Spawn(@"main\scripts\generic\tunnel-server.cs", @"main\scripts\specific\tunnel-server-config.cs", Wd);
await Console.Out.WriteLineAsync("server pid=" + Server.Id);
await Task.Delay(5000);
var Host = Spawn(@"main\scripts\generic\tunnel-host.cs", @"main\scripts\specific\tunnel-host-config.cs", Wd);
await Console.Out.WriteLineAsync("host pid=" + Host.Id);
await Task.Delay(15000);
var Tester = Spawn(@"main\scripts\generic\tunnel-client.cs", @"main\scripts\specific\tunnel-client-tester-cloud-config.cs", Wd);
await Console.Out.WriteLineAsync("tester pid=" + Tester.Id);
await Task.Delay(8000);
var Developer = Spawn(@"main\scripts\generic\tunnel-client.cs", @"main\scripts\specific\tunnel-client-developer-cloud-config.cs", Wd);
await Console.Out.WriteLineAsync("developer pid=" + Developer.Id);
await Task.Delay(8000);
await Console.Out.WriteLineAsync("tunnel-up: all 4 processes detached and running. Use kill-by-port + kill-process to take down.");
return 0;
