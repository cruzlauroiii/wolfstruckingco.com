#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;

var Specs = await File.ReadAllLinesAsync(SpecPath);
string? Read(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        if (After.StartsWith("@", StringComparison.Ordinal)) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}
int ReadInt(string Name, int Default)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const int " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 10 + Name.Length + 3);
        var Semi = After.IndexOf(";", StringComparison.Ordinal);
        if (Semi < 0) continue;
        if (int.TryParse(After.Substring(0, Semi), out var V)) return V;
    }
    return Default;
}

var Url = Read("Url");
var OutputPath = Read("OutputPath");
var Selector = Read("Selector") ?? "";
var TypeText = Read("TypeText") ?? "";
var Repo = Read("Repo") ?? Environment.CurrentDirectory;
var HydrateMs = ReadInt("HydrateMs", 6000);
var PostClickMs = ReadInt("PostClickMs", 2500);
if (Url is null || OutputPath is null) return 3;

var AuthDomains = new[] { "accounts.google.com", "login.microsoftonline.com", "github.com/login", "login.okta.com", ".okta.com" };
var CdpGeneric = "main/scripts/generic/chrome-devtools.cs";

string EscString(string S) => S.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

async Task<(int Exit, string Captured)> RunCdp(string Body)
{
    var TempCfg = Path.Combine(Path.GetTempPath(), $"cdp-real-{Guid.NewGuid():N}.cs");
    var TempLog = Path.Combine(Path.GetTempPath(), $"cdp-real-log-{Guid.NewGuid():N}.txt");
    var Full = $"return 0;\nnamespace Scripts\n{{\n    internal static class CdpRun\n    {{\n        {Body}\n        public const string OutputPath = \"{EscString(TempLog)}\";\n    }}\n}}\n";
    await File.WriteAllTextAsync(TempCfg, Full);
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Repo };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(CdpGeneric);
    Psi.ArgumentList.Add(TempCfg);
    using var P = Process.Start(Psi)!;
    var ExitTask = P.WaitForExitAsync();
    var Done = await Task.WhenAny(ExitTask, Task.Delay(120000));
    if (Done != ExitTask) { try { P.Kill(true); } catch { } try { File.Delete(TempCfg); } catch { } return (-1, ""); }
    var Captured = File.Exists(TempLog) ? await File.ReadAllTextAsync(TempLog) : "";
    try { File.Delete(TempCfg); } catch { }
    try { File.Delete(TempLog); } catch { }
    return (P.ExitCode, Captured);
}

var Nav = await RunCdp($"public const string Command = \"navigate_page\"; public const string Url = \"{EscString(Url)}\";");
if (Nav.Exit != 0) return 4;

await Task.Delay(HydrateMs);

var EvalUrl = await RunCdp($"public const string Command = \"evaluate_script\"; public const string Script = \"location.href\";");
if (EvalUrl.Exit != 0) return 5;
var CurUrl = EvalUrl.Captured;
foreach (var Auth in AuthDomains)
{
    if (CurUrl.Contains(Auth, StringComparison.OrdinalIgnoreCase)) return 42;
}

if (!string.IsNullOrEmpty(Selector))
{
    string Body;
    if (!string.IsNullOrEmpty(TypeText))
    {
        Body = $"public const string Command = \"fill\"; public const string Selector = \"{EscString(Selector)}\"; public const string Text = \"{EscString(TypeText)}\";";
    }
    else
    {
        Body = $"public const string Command = \"click\"; public const string Selector = \"{EscString(Selector)}\";";
    }
    var R = await RunCdp(Body);
    if (R.Exit != 0) return 6;
    await Task.Delay(PostClickMs);
}

var OutDir = Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(OutDir)) Directory.CreateDirectory(OutDir);
var Shot = await RunCdp($"public const string Command = \"take_screenshot\"; public const string FilePath = \"{EscString(OutputPath)}\";");
if (Shot.Exit != 0) return 7;
if (!File.Exists(OutputPath)) return 8;
return 0;
