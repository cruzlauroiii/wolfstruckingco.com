#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run ffprobe-duration.cs <config>"); return 1; }
var Spec = await File.ReadAllTextAsync(args[0]);
string Get(string Name) { var M = Regex.Match(Spec, "const\\s+string\\s+" + Name + "\\s*=\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\""); return M.Success ? M.Groups[1].Value : ""; }
var FilePath = Get("FilePath");
if (!File.Exists(FilePath)) { Console.Error.WriteLine($"missing: {FilePath}"); return 2; }
var Psi = new ProcessStartInfo("ffprobe") { RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", FilePath }) Psi.ArgumentList.Add(A);
using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync();
var Err = await P.StandardError.ReadToEndAsync();
await P.WaitForExitAsync();
if (!string.IsNullOrEmpty(Err)) Console.Error.Write(Err);
var Dur = double.Parse(Out.Trim(), System.Globalization.CultureInfo.InvariantCulture);
var Min = (int)(Dur / 60);
var Sec = Dur - Min * 60;
Console.WriteLine($"{FilePath}: {Min}m {Sec:F1}s ({Dur:F1}s total)");
return 0;
