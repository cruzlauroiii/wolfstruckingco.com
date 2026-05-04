#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name, string fallback = "")
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : fallback;
}

var source = Get("Source");
var target = Get("Target");
if (!File.Exists(source) || string.IsNullOrWhiteSpace(target)) return 2;
var dir = Path.GetDirectoryName(target);
if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
File.Copy(source, target, overwrite: true);
Console.WriteLine(target);
return 0;
