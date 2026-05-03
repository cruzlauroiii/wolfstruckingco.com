#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;
using System.Text.Json;

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

var ScenesPath = Read("ScenesPath");
var FrameDir = Read("FrameDir");
var Repo = Read("Repo") ?? Environment.CurrentDirectory;
var RealRenderGeneric = Read("RealRenderGeneric") ?? "main/scripts/generic/real-render.cs";
var RequestHumanGeneric = Read("RequestHumanGeneric") ?? "main/scripts/generic/request-human.cs";
var RequestHumanConfig = Read("RequestHumanConfig") ?? "main/scripts/specific/request-human-scratch-config.cs";
var HydrateMs = ReadInt("HydrateMs", 6000);
if (ScenesPath is null || FrameDir is null) return 3;
if (!File.Exists(ScenesPath)) return 4;
Directory.CreateDirectory(FrameDir);
foreach (var F in Directory.GetFiles(FrameDir, "*.png")) File.Delete(F);

string EscString(string S) => S.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

async Task<int> RunRender(string Url, string OutputPath, string Selector, string TypeText)
{
    var Body = $"return 0;\nnamespace Scripts\n{{\n    internal static class RealRenderRun\n    {{\n        public const string Url = \"{EscString(Url)}\";\n        public const string OutputPath = \"{EscString(OutputPath)}\";\n        public const string Selector = \"{EscString(Selector)}\";\n        public const string TypeText = \"{EscString(TypeText)}\";\n        public const int HydrateMs = {HydrateMs};\n        public const int PostClickMs = 2500;\n    }}\n}}\n";
    var TempCfg = Path.Combine(Path.GetTempPath(), $"real-render-cfg-{Guid.NewGuid():N}.cs");
    await File.WriteAllTextAsync(TempCfg, Body);
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Repo };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(RealRenderGeneric);
    Psi.ArgumentList.Add(TempCfg);
    using var P = Process.Start(Psi)!;
    var ExitTask = P.WaitForExitAsync();
    var Done = await Task.WhenAny(ExitTask, Task.Delay(180000));
    if (Done != ExitTask) { try { P.Kill(true); } catch { } return -1; }
    var StdOut = await P.StandardOutput.ReadToEndAsync();
    var StdErr = await P.StandardError.ReadToEndAsync();
    if (P.ExitCode != 0) { await Console.Error.WriteLineAsync($"render cfg {TempCfg} exit {P.ExitCode}\nSTDOUT: {StdOut}\nSTDERR: {StdErr}"); }
    return P.ExitCode;
}

async Task<int> RequestHuman()
{
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Repo };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(RequestHumanGeneric);
    Psi.ArgumentList.Add(RequestHumanConfig);
    using var P = Process.Start(Psi)!;
    await P.WaitForExitAsync();
    return P.ExitCode;
}

var ScenesJson = JsonDocument.Parse(await File.ReadAllTextAsync(ScenesPath));
var Scenes = ScenesJson.RootElement.EnumerateArray().ToArray();
for (var I = 0; I < Scenes.Length; I++)
{
    var Scene = Scenes[I];
    var Pad = Scene.GetProperty("pad").GetString() ?? (I + 1).ToString("000", System.Globalization.CultureInfo.InvariantCulture);
    var Url = Scene.GetProperty("url").GetString() ?? "";
    var Selector = Scene.TryGetProperty("selector", out var Se) ? Se.GetString() ?? "" : "";
    var TypeText = Scene.TryGetProperty("typeText", out var Tt) ? Tt.GetString() ?? "" : "";
    var FramePath = Path.Combine(FrameDir, $"{Pad}.png");

    var Exit = await RunRender(Url, FramePath, Selector, TypeText);
    if (Exit == 42)
    {
        await RequestHuman();
        Exit = await RunRender(Url, FramePath, Selector, TypeText);
    }
    if (Exit != 0 || !File.Exists(FramePath))
    {
        await Console.Error.WriteLineAsync($"scene {Pad} failed exit {Exit} framepath-exists {File.Exists(FramePath)}");
        return 8;
    }
}
return 0;
