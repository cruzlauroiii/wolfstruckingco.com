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

var target = Get("Target", ".");
var configuration = Get("Configuration", "Release");
var timeoutMs = int.Parse(Get("TimeoutMs", "240000"));
var psi = new ProcessStartInfo("dotnet", $"build \"{target}\" -c {configuration} --nologo")
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};
using var process = Process.Start(psi) ?? throw new InvalidOperationException("dotnet build did not start");
var outputTask = process.StandardOutput.ReadToEndAsync();
var errorTask = process.StandardError.ReadToEndAsync();
if (!process.WaitForExit(timeoutMs))
{
    process.Kill(entireProcessTree: true);
    Console.WriteLine("dotnet build timed out");
    return 124;
}
var output = await outputTask;
var error = await errorTask;
Console.Write(output);
Console.Error.Write(error);
return process.ExitCode;
