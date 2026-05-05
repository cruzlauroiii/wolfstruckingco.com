#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run gen-scene-configs.cs <config>"); return 1; }
var Spec = await File.ReadAllTextAsync(args[0]);
string Get(string Name) { var M = Regex.Match(Spec, "const\\s+string\\s+" + Name + "\\s*=\\s*@?\"((?:[^\"\\\\]|\\\\.)*)\""); return M.Success ? M.Groups[1].Value : ""; }

var ScenesPath = Get("ScenesPath");
var OutDir = Get("OutDir");
Directory.CreateDirectory(OutDir);
var ScenesText = await File.ReadAllTextAsync(ScenesPath);
var Doc = JsonDocument.Parse(ScenesText);
int Idx = 0;
int Count = 0;
foreach (var Scene in Doc.RootElement.EnumerateArray())
{
    Idx++;
    var Target = Scene.GetProperty("target").GetString() ?? "";
    var Narration = Scene.GetProperty("narration").GetString() ?? "";
    string Pad;
    if (Target.Contains("cb=")) { var Cb = Target.Split("cb=")[^1].Replace("?", "").Replace("/", "").Trim(); Pad = Cb.Substring(0, Math.Min(3, Cb.Length)); }
    else Pad = Idx.ToString("D3");
    var Selector = "#app > .TopBar";
    string BeforeShot = "";
    if (Pad == "001") { Selector = "#app > .TopBar a[href*=Login]"; BeforeShot = "() => { try { ['wolfs_role','wolfs_email','wolfs_session','wolfs_sso'].forEach(function(k){localStorage.removeItem(k);}); } catch(e){} location.reload(); return 'cleared'; }"; }
    var EscNarration = Narration.Replace("\\", "\\\\").Replace("\"", "\\\"");
    var Body = "return 0;\n\nnamespace Scripts\n{\n    internal static class SceneConfig\n    {\n        public const string Pad = \"" + Pad + "\";\n        public const string Url = \"" + Target + "\";\n        public const string HydrateSelector = \"" + Selector + "\";\n        public const string BeforeShotJs = \"" + BeforeShot.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\";\n        public const string Narration = \"" + EscNarration + "\";\n    }\n}\n";
    var OutPath = Path.Combine(OutDir, $"scene-{Pad}-config.cs");
    await File.WriteAllTextAsync(OutPath, Body);
    Count++;
}
Console.WriteLine($"Generated {Count} scene configs in {OutDir}");
return 0;
