#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Net.Http;

var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, WorkingDirectory = @"C:\repo\public\wolfstruckingco.com", CreateNoWindow = false };
Psi.ArgumentList.Add("run");
Psi.ArgumentList.Add(@"main\scripts\generic\chrome-devtools.cs");
Psi.ArgumentList.Add(@"main\scripts\specific\chrome-devtools-serve-config.cs");
var Serve = Process.Start(Psi)!;
await Console.Out.WriteLineAsync("daemon spawned pid=" + Serve.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
for (var I = 0; I < 60; I++)
{
    try
    {
        var R = await Http.PostAsync(new Uri("http://127.0.0.1:9334/"), new StringContent("list_pages"));
        if (R.IsSuccessStatusCode)
        {
            await Console.Out.WriteLineAsync("daemon ready after " + (I + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " attempts; not killing — daemon will run detached.");
            return 0;
        }
    }
    catch { }
    await Task.Delay(1000);
}
await Console.Error.WriteLineAsync("daemon failed to start within 60s");
try { Serve.Kill(true); } catch { }
return 1;
