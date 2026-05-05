#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run bulk-replace.cs <config>"); return 1; }
var Spec = await File.ReadAllTextAsync(args[0]);
string Get(string Name) { var M = Regex.Match(Spec, "const\\s+string\\s+" + Name + "\\s*=\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\""); return M.Success ? M.Groups[1].Value : ""; }

var RootDir = Get("RootDir");
var Glob = Get("Glob");
var Find = Regex.Unescape(Get("Find"));
var Replace = Regex.Unescape(Get("Replace"));
if (string.IsNullOrEmpty(RootDir) || string.IsNullOrEmpty(Glob) || string.IsNullOrEmpty(Find)) { Console.Error.WriteLine("missing RootDir/Glob/Find"); return 2; }

int touched = 0, changed = 0;
foreach (var File1 in Directory.EnumerateFiles(RootDir, Glob, SearchOption.AllDirectories))
{
    touched++;
    var Body = await File.ReadAllTextAsync(File1);
    if (!Body.Contains(Find, StringComparison.Ordinal)) continue;
    var New = Body.Replace(Find, Replace);
    await File.WriteAllTextAsync(File1, New);
    changed++;
}
Console.WriteLine($"bulk-replace touched={touched} changed={changed}");
return 0;
