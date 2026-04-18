#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using Scripts;

const string JsPath = $@"{Paths.Repo}\worker\worker.js";
const string CsPath = $@"{Paths.Repo}\worker\worker.cs";

if (!File.Exists(JsPath)) { await Console.Error.WriteLineAsync($"already gone: {JsPath}"); return 1; }
var BodyBytes = await File.ReadAllBytesAsync(JsPath);
var B64 = Convert.ToBase64String(BodyBytes);
const int LineLen = 100;
var Sb = new System.Text.StringBuilder();
for (var I = 0; I < B64.Length; I += LineLen)
{
    Sb.Append("    \"").Append(B64.AsSpan(I, Math.Min(LineLen, B64.Length - I))).Append("\",\n");
}
var Escaped = JsPath.Replace("\\", "\\\\", StringComparison.Ordinal);
var Lines = new List<string>
{
    "#:property TargetFramework=net11.0",
    "#:property RunAnalyzersDuringBuild=false",
    "#:property TreatWarningsAsErrors=false",
    "#:property EnforceCodeStyleInBuild=false",
    string.Empty,
    $"const string Path = \"{Escaped}\";",
    "var B64 = $\"{string.Join(string.Empty, JsBody())}\";",
    "var Bytes = Convert.FromBase64String(B64);",
    "File.WriteAllBytes(Path, Bytes);",
    "return 0;",
    string.Empty,
    "static string[] JsBody() => new[]",
    "{",
    Sb.ToString().TrimEnd('\n'),
    "};",
    string.Empty,
};
var Cs = string.Join("\n", Lines);
await File.WriteAllTextAsync(CsPath, Cs);
File.Delete(JsPath);
await Console.Out.WriteLineAsync($"wrote {CsPath}");
return 0;
