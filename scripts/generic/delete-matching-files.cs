#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name)
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : "";
}
var root = Get("Root");
var pattern = Get("Pattern");
if (!Directory.Exists(root) || string.IsNullOrWhiteSpace(pattern)) return 2;
foreach (var file in Directory.GetFiles(root, pattern))
{
    File.Delete(file);
    Console.WriteLine("Deleted " + Path.GetFileName(file));
}
return 0;
