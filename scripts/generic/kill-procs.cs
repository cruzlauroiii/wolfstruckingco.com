#:property TargetFramework=net11.0
#:property TreatWarningsAsErrors=false
#:property RunAnalyzersDuringBuild=false
#:property EnforceCodeStyleInBuild=false
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true

using System.Diagnostics;
using System.Globalization;

var ConfigPath = args.Length > 0 ? args[0] : "main/scripts/specific/kill-procs-git-config.cs";
var ConfigSrc = await File.ReadAllTextAsync(ConfigPath);

var ProcessNames = new[] { "git", "dotnet" };
var OnlyOlderThanSec = 0;

var NameMatch = System.Text.RegularExpressions.Regex.Match(ConfigSrc, "public\\s+static\\s+readonly\\s+string\\[\\]\\s+ProcessNames\\s*=\\s*new\\[\\]\\s*\\{([^}]*)\\}");
if (NameMatch.Success)
{
    var Body = NameMatch.Groups[1].Value;
    ProcessNames = System.Text.RegularExpressions.Regex.Matches(Body, "\"([^\"]*)\"").Select(M => M.Groups[1].Value).ToArray();
}
var SecMatch = System.Text.RegularExpressions.Regex.Match(ConfigSrc, "public\\s+const\\s+int\\s+OnlyOlderThanSec\\s*=\\s*(\\d+)");
if (SecMatch.Success) { OnlyOlderThanSec = int.Parse(SecMatch.Groups[1].Value, CultureInfo.InvariantCulture); }

var Self = Environment.ProcessId;
var Killed = 0;
var Cutoff = DateTime.Now.AddSeconds(-OnlyOlderThanSec);

foreach (var P in Process.GetProcesses())
{
    try
    {
        if (P.Id == Self) { continue; }
        var Name = P.ProcessName;
        if (!ProcessNames.Any(N => string.Equals(N, Name, StringComparison.OrdinalIgnoreCase))) { continue; }
        try
        {
            if (OnlyOlderThanSec > 0 && P.StartTime > Cutoff) { continue; }
        }
        catch (InvalidOperationException) { continue; }
        catch (System.ComponentModel.Win32Exception) { continue; }
        P.Kill();
        Killed++;
    }
    catch (InvalidOperationException) { }
    catch (System.ComponentModel.Win32Exception) { }
}
if (Killed > 0) { await Console.Out.WriteLineAsync($"killed {Killed.ToString(CultureInfo.InvariantCulture)}"); }
return 0;
