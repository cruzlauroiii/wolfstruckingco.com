#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

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

var PathToPrint = Get("Path");
var MaxChars = int.Parse(Get("MaxChars", "12000"));
if (string.IsNullOrWhiteSpace(PathToPrint)) return 3;
if (!File.Exists(PathToPrint)) return 4;
var Text = await File.ReadAllTextAsync(PathToPrint);
Console.Write(Text.Length > MaxChars ? Text[..MaxChars] : Text);
return 0;
