#:property TargetFramework=net11.0-windows
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
var match = Regex.Match(spec, @"const\s+string\s+Contains\s*=\s*""(?<v>[^""]*)""");
if (!match.Success) return 2;
var needle = match.Groups["v"].Value;
var escaped = needle.Replace("'", "''");
var ps = "$me=$PID; Get-CimInstance Win32_Process -Filter \"name = 'dotnet.exe'\" | " +
    "Where-Object { $_.ProcessId -ne $me -and $_.CommandLine -like '*" + escaped + "*' } | " +
    "ForEach-Object { Stop-Process -Id $_.ProcessId -Force; Write-Output ('Killed dotnet PID ' + $_.ProcessId) }";
var start = new ProcessStartInfo("powershell")
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};
start.ArgumentList.Add("-NoProfile");
start.ArgumentList.Add("-Command");
start.ArgumentList.Add(ps);
using var child = Process.Start(start)!;
Console.Write(await child.StandardOutput.ReadToEndAsync());
Console.Error.Write(await child.StandardError.ReadToEndAsync());
await child.WaitForExitAsync();
return child.ExitCode;
