#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name, string fallback = "")
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : fallback;
}

var path = Get("Path");
if (!File.Exists(path)) return 2;
var psi = new ProcessStartInfo("ffprobe")
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};
foreach (var arg in new[] { "-v", "error", "-show_entries", "format=duration,size", "-of", "default=nw=1", path })
{
    psi.ArgumentList.Add(arg);
}
using var p = Process.Start(psi) ?? throw new InvalidOperationException("ffprobe failed");
var output = await p.StandardOutput.ReadToEndAsync();
var error = await p.StandardError.ReadToEndAsync();
await p.WaitForExitAsync();
Console.Write(output);
Console.Error.Write(error);
return p.ExitCode;
