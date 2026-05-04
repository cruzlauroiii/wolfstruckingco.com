#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name, string fallback = "")
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : fallback;
}

var repo = Get("Repo", Environment.CurrentDirectory);
var command = Get("Command");
if (string.IsNullOrWhiteSpace(command)) return 2;
var parts = command.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var psi = new ProcessStartInfo("git")
{
    WorkingDirectory = repo,
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true
};
foreach (var part in parts) psi.ArgumentList.Add(part);
using var p = Process.Start(psi) ?? throw new InvalidOperationException("git failed");
var stdout = p.StandardOutput.ReadToEndAsync();
var stderr = p.StandardError.ReadToEndAsync();
var wait = p.WaitForExitAsync();
if (await Task.WhenAny(wait, Task.Delay(300000)) != wait)
{
    p.Kill(entireProcessTree: true);
    return 124;
}
Console.Write(await stdout);
Console.Error.Write(await stderr);
return p.ExitCode;
