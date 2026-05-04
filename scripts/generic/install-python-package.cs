#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Spec = await File.ReadAllTextAsync(SpecPath);

string Get(string Name, string Default = "")
{
    var Match = Regex.Match(Spec, @"const\s+string\s+" + Name + @"\s*=\s*@?""(?<v>[^""]*)""\s*;");
    return Match.Success ? Match.Groups["v"].Value : Default;
}

var Package = Get("Package");
var TimeoutMs = int.Parse(Get("TimeoutMs", "900000"));
if (string.IsNullOrWhiteSpace(Package)) return 3;

var Psi = new ProcessStartInfo("python")
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
};
Psi.ArgumentList.Add("-m");
Psi.ArgumentList.Add("pip");
Psi.ArgumentList.Add("install");
foreach (var Part in Regex.Matches(Package, @"[^\s""]+|""([^""]*)""").Select(M => M.Value.Trim('"')))
{
    if (!string.IsNullOrWhiteSpace(Part)) Psi.ArgumentList.Add(Part);
}

using var Child = Process.Start(Psi)!;
var OutputTask = Child.StandardOutput.ReadToEndAsync();
var ErrorTask = Child.StandardError.ReadToEndAsync();
var ExitTask = Child.WaitForExitAsync();
if (await Task.WhenAny(ExitTask, Task.Delay(TimeoutMs)) != ExitTask)
{
    try { Child.Kill(true); } catch { }
    Console.Error.WriteLine("pip install timed out: " + Package);
    return 124;
}

var Output = await OutputTask;
var Error = await ErrorTask;
if (!string.IsNullOrWhiteSpace(Output)) Console.Write(Output);
if (!string.IsNullOrWhiteSpace(Error)) Console.Error.Write(Error);
return Child.ExitCode;
