#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Net.Http;

await Console.Out.WriteLineAsync("[probe] starting");
var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, WorkingDirectory = @"C:\repo\public\wolfstruckingco.com", CreateNoWindow = false };
Psi.ArgumentList.Add("run");
Psi.ArgumentList.Add(@"main\scripts\generic\chrome-devtools.cs");
Psi.ArgumentList.Add(@"main\scripts\specific\chrome-devtools-serve-config.cs");
var Serve = Process.Start(Psi)!;
await Console.Out.WriteLineAsync("[probe] spawned daemon pid=" + Serve.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
for (var I = 0; I < 60; I++)
{
    await Console.Out.WriteLineAsync("[probe] attempt " + (I + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
    try
    {
        var R = await Http.PostAsync(new Uri("http://127.0.0.1:9334/"), new StringContent("list_pages"));
        var Body = await R.Content.ReadAsStringAsync();
        await Console.Out.WriteLineAsync("[probe] OK status=" + ((int)R.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture) + " body=" + (Body.Length > 200 ? Body[..200] : Body));
        try { Serve.Kill(true); } catch { }
        return 0;
    }
    catch (Exception E)
    {
        await Console.Out.WriteLineAsync("[probe] err: " + E.GetType().Name + ": " + E.Message);
    }
    await Task.Delay(1000);
}
await Console.Out.WriteLineAsync("[probe] gave up after 60 attempts");
try { Serve.Kill(true); } catch { }
return 1;
