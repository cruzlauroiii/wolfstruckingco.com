#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run run-all-scenes.cs <config>"); return 1; }
var Spec = await File.ReadAllTextAsync(args[0]);
string Get(string Name) { var M = Regex.Match(Spec, "const\\s+string\\s+" + Name + "\\s*=\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\""); return M.Success ? M.Groups[1].Value : ""; }

var Repo = Get("Repo");
var SpecificDir = Get("SpecificDir");
var From = int.Parse(Get("From"));
var To = int.Parse(Get("To"));

int Ok = 0, Fail = 0;
var Failed = new List<string>();
for (int I = From; I <= To; I++)
{
    var Pad = I.ToString("D3");
    var Cfg = Path.Combine(SpecificDir, $"scene-{Pad}-config.cs");
    if (!File.Exists(Cfg)) { Console.WriteLine($"skip {Pad} (no config)"); continue; }
    var Psi = new ProcessStartInfo("dotnet") { WorkingDirectory = Repo, RedirectStandardOutput = true, RedirectStandardError = true };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(Path.Combine(Repo, "scripts", "generic", "run-scene.cs"));
    Psi.ArgumentList.Add(Cfg);
    using var P = Process.Start(Psi)!;
    var O = P.StandardOutput.ReadToEndAsync(); var E = P.StandardError.ReadToEndAsync(); var W = P.WaitForExitAsync();
    if (await Task.WhenAny(W, Task.Delay(180000)) != W) { try { P.Kill(true); } catch {} Console.WriteLine($"  {Pad} TIMEOUT"); Fail++; Failed.Add(Pad); continue; }
    await Task.WhenAll(O, E);
    if (P.ExitCode == 0) { Console.WriteLine($"  {Pad} ok"); Ok++; }
    else { Console.WriteLine($"  {Pad} FAIL rc={P.ExitCode}"); Fail++; Failed.Add(Pad); }
}
Console.WriteLine($"DONE ok={Ok} fail={Fail}");
if (Failed.Count > 0) Console.WriteLine($"failed pads: {string.Join(",", Failed)}");
return Fail > 0 ? 5 : 0;
