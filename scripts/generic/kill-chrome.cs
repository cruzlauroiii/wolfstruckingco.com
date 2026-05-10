#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;

var Killed = 0;
foreach (var P in Process.GetProcessesByName("chrome"))
{
    try { P.Kill(true); Killed++; } catch { }
    P.Dispose();
}
foreach (var P in Process.GetProcessesByName("chrome-devtools"))
{
    try { P.Kill(true); Killed++; } catch { }
    P.Dispose();
}
await Console.Error.WriteLineAsync("kill-chrome: killed=" + Killed.ToString(CultureInfo.InvariantCulture));
return 0;
