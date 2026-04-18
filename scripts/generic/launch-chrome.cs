#:property TargetFramework=net11.0-windows
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
string StringFromConfig(string Name, string Default)
{
    var M = Regex.Match(Body, @"const\s+string\s+" + Name + "\\s*=\\s*\"(?<v>[^\"]*)\"\\s*;", RegexOptions.ExplicitCapture);
    return M.Success ? M.Groups["v"].Value : Default;
}
int IntFromConfig(string Name, int Default)
{
    var M = Regex.Match(Body, @"const\s+int\s+" + Name + @"\s*=\s*(?<v>-?\d+)\s*;", RegexOptions.ExplicitCapture);
    return M.Success ? int.Parse(M.Groups["v"].Value, System.Globalization.CultureInfo.InvariantCulture) : Default;
}

var ChromePathRel = StringFromConfig("ChromePathRel", "Google\\Chrome\\Application\\chrome.exe");
var Arg1 = StringFromConfig("Arg1", "--start-maximized");
var Arg2 = StringFromConfig("Arg2", "--remote-allow-origins=*");
var Arg3 = StringFromConfig("Arg3", "");
var WaitMs = IntFromConfig("WaitMs", 6000);

var ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
var ChromePath = ProgramFiles;
foreach (var Part in ChromePathRel.Split('\\')) { ChromePath = Path.Combine(ChromePath, Part); }
if (!File.Exists(ChromePath)) { await Console.Error.WriteLineAsync($"Chrome not found at {ChromePath}"); return 3; }

if (Process.GetProcessesByName("chrome").Length > 0) { return 0; }

var Psi = new ProcessStartInfo
{
    FileName = ChromePath,
    UseShellExecute = true,
    WindowStyle = ProcessWindowStyle.Maximized,
};
if (!string.IsNullOrEmpty(Arg1)) { Psi.ArgumentList.Add(Arg1); }
if (!string.IsNullOrEmpty(Arg2)) { Psi.ArgumentList.Add(Arg2); }
if (!string.IsNullOrEmpty(Arg3)) { Psi.ArgumentList.Add(Arg3); }

Process.Start(Psi);
await Task.Delay(WaitMs);

var ChromeUserDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
var ActivePortFile = Path.Combine(ChromeUserDataDir, "DevToolsActivePort");
for (var I = 0; I < 30; I++)
{
    if (File.Exists(ActivePortFile))
    {
        var Lines = await File.ReadAllLinesAsync(ActivePortFile);
        if (Lines.Length >= 2 && int.TryParse(Lines[0].Trim(), out _)) { return 0; }
    }
    await Task.Delay(500);
}
await Console.Error.WriteLineAsync("DevToolsActivePort never appeared with valid port");
return 4;
