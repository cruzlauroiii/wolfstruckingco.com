#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Net.Http;

var UserData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
var PortFile = Path.Combine(UserData, "DevToolsActivePort");
var OutPath = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\probe-debug.out";
var Sb = new System.Text.StringBuilder();
Sb.AppendLine("PortFile: " + PortFile);
Sb.AppendLine("Exists: " + File.Exists(PortFile));
if (File.Exists(PortFile))
{
    var Lines = await File.ReadAllLinesAsync(PortFile);
    Sb.AppendLine("Lines: " + Lines.Length);
    foreach (var L in Lines) Sb.AppendLine("  | " + L);
    if (Lines.Length > 0)
    {
        var Port = Lines[0];
        using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        foreach (var Path1 in new[] { "/json/version", "/json/list", "/json" })
        {
            try { var R = await Http.GetAsync(new Uri("http://127.0.0.1:" + Port + Path1)); Sb.AppendLine($"{Path1}: status={(int)R.StatusCode}"); var B = await R.Content.ReadAsStringAsync(); Sb.AppendLine($"  body[0..200]: {B[..Math.Min(B.Length, 200)]}"); }
            catch (Exception E) { Sb.AppendLine($"{Path1}: EX {E.GetType().Name}: {E.Message}"); }
        }
    }
}
await File.WriteAllTextAsync(OutPath, Sb.ToString());
return 0;
