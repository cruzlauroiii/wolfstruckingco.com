#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run wait-sentinel.cs sentinel-config.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { Console.Error.WriteLine($"missing: {SpecPath}"); return 2; }
var Spec = await File.ReadAllTextAsync(SpecPath);
string Get(string Name)
{
    var Rx = new Regex("const\\s+string\\s+" + Name + "\\s*=\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\"");
    var M = Rx.Match(Spec);
    return M.Success ? M.Groups[1].Value : "";
}
int GetInt(string Name, int Def)
{
    var Rx = new Regex("const\\s+int\\s+" + Name + "\\s*=\\s*(-?\\d+)");
    var M = Rx.Match(Spec);
    return M.Success ? int.Parse(M.Groups[1].Value) : Def;
}

var Path_ = Get("SentinelPath");
var FailMarker = Get("FailMarker");
if (string.IsNullOrEmpty(FailMarker)) FailMarker = "FAILED";
var TimeoutSeconds = GetInt("TimeoutSeconds", 300);
if (string.IsNullOrEmpty(Path_)) { Console.Error.WriteLine("SentinelPath required"); return 3; }

var Deadline = DateTime.UtcNow.AddSeconds(TimeoutSeconds);
while (DateTime.UtcNow < Deadline)
{
    if (File.Exists(Path_))
    {
        try
        {
            var C = (await File.ReadAllTextAsync(Path_)).Trim();
            if (C.Equals(FailMarker, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"sentinel FAILED: {Path_}");
                return 4;
            }
            if (!string.IsNullOrEmpty(C)) return 0;
        }
        catch { }
    }
    await Task.Delay(1000);
}
Console.Error.WriteLine($"sentinel TIMEOUT: {Path_}");
return 5;
