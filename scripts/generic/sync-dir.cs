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
var glob = Get("Glob", "*.*");
if (!Directory.Exists(source)) return 2;
Directory.CreateDirectory(target);
var count = 0;
foreach (var file in Directory.GetFiles(source, glob, SearchOption.TopDirectoryOnly))
{
    var dest = Path.Combine(target, Path.GetFileName(file));
    File.Copy(file, dest, overwrite: true);
    count++;
}
Console.WriteLine("copied=" + count);
Console.WriteLine("target=" + target);
return 0;
