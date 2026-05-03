#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;
using System.Text.Json;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

string? RoutesPath = null;
string? FrameDir = null;
string? Repo = null;
string? CdpGenericPath = null;
int HydrateMs = 2500;
foreach (var Line in await File.ReadAllLinesAsync(SpecPath))
{
    var SIdx = Line.IndexOf("const string ", StringComparison.Ordinal);
    if (SIdx >= 0)
    {
        var After = Line.Substring(SIdx + 13);
        var Eq = After.IndexOf(" = ", StringComparison.Ordinal);
        if (Eq < 0) continue;
        var Name = After.Substring(0, Eq).Trim();
        var Rhs = After.Substring(Eq + 3).TrimStart();
        if (Rhs.StartsWith("@", StringComparison.Ordinal)) Rhs = Rhs.Substring(1);
        if (!Rhs.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = Rhs.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        var Value = Rhs.Substring(1, End - 1);
        if (Name == "RoutesPath") RoutesPath = Value;
        else if (Name == "FrameDir") FrameDir = Value;
        else if (Name == "Repo") Repo = Value;
        else if (Name == "CdpGenericPath") CdpGenericPath = Value;
    }
    var IIdx = Line.IndexOf("const int ", StringComparison.Ordinal);
    if (IIdx >= 0)
    {
        var After = Line.Substring(IIdx + 10);
        var Eq = After.IndexOf(" = ", StringComparison.Ordinal);
        if (Eq < 0) continue;
        var Name = After.Substring(0, Eq).Trim();
        var Rhs = After.Substring(Eq + 3).TrimStart();
        var Semi = Rhs.IndexOf(";", StringComparison.Ordinal);
        if (Semi < 0) continue;
        if (int.TryParse(Rhs.Substring(0, Semi), out var V) && Name == "HydrateMs") HydrateMs = V;
    }
}
if (RoutesPath is null || FrameDir is null || Repo is null || CdpGenericPath is null) return 3;
if (!File.Exists(RoutesPath)) return 4;
Directory.CreateDirectory(FrameDir);
foreach (var F in Directory.GetFiles(FrameDir, "*.png")) File.Delete(F);

var Routes = JsonDocument.Parse(await File.ReadAllTextAsync(RoutesPath)).RootElement.EnumerateArray().Select(E => E.GetString() ?? "").Where(S => !string.IsNullOrEmpty(S)).ToArray();
var TempDir = Path.Combine(Path.GetTempPath(), "wolfs-walkthrough", "cdp-configs");
Directory.CreateDirectory(TempDir);

var KeywordRe = new System.Text.RegularExpressions.Regex("\\b(warning|error)\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
async Task<int> RunCapture(string Generic, string ConfigPath)
{
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Repo };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(Generic);
    Psi.ArgumentList.Add(ConfigPath);
    using var P = Process.Start(Psi)!;
    var Killed = false;
    string? OffendingLine = null;
    async Task Stream(StreamReader Sr)
    {
        string? Ln;
        while ((Ln = await Sr.ReadLineAsync()) is not null)
        {
            if (KeywordRe.IsMatch(Ln) && !Killed)
            {
                Killed = true;
                OffendingLine = Ln;
                try { P.Kill(true); } catch { }
                return;
            }
        }
    }
    var T1 = Stream(P.StandardOutput);
    var T2 = Stream(P.StandardError);
    var ExitTask = P.WaitForExitAsync();
    var Done = await Task.WhenAny(ExitTask, Task.Delay(90000));
    if (Done != ExitTask) { try { P.Kill(true); } catch { } return -1; }
    await Task.WhenAll(T1, T2);
    if (Killed) { await Console.Error.WriteLineAsync($"warning/error: {OffendingLine?.Trim()}"); return -2; }
    return P.ExitCode;
}

for (var I = 0; I < Routes.Length; I++)
{
    var Url = Routes[I];
    var Pad = (I + 1).ToString("000", System.Globalization.CultureInfo.InvariantCulture);
    var NavCfg = Path.Combine(TempDir, $"nav-{Pad}.cs");
    var ShotCfg = Path.Combine(TempDir, $"shot-{Pad}.cs");
    var FramePath = Path.Combine(FrameDir, $"{Pad}.png");
    var BustUrl = Url + (Url.Contains("?", StringComparison.Ordinal) ? "&" : "?") + "cb=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    await File.WriteAllTextAsync(NavCfg, $"namespace Specific;\npublic static class CdpNav{Pad}\n{{\n    public const string Command = \"new_page\";\n    public const string Url = \"{BustUrl}\";\n}}\n");
    await File.WriteAllTextAsync(ShotCfg, $"namespace Specific;\npublic static class CdpShot{Pad}\n{{\n    public const string Command = \"take_screenshot\";\n    public const string FilePath = @\"{FramePath}\";\n}}\n");
    var NavExit = await RunCapture(CdpGenericPath, NavCfg);
    if (NavExit != 0) { await Console.Error.WriteLineAsync($"nav failed scene {Pad}: {Url}"); return 5; }
    await Task.Delay(HydrateMs);
    var ShotExit = await RunCapture(CdpGenericPath, ShotCfg);
    if (ShotExit != 0 || !File.Exists(FramePath)) { await Console.Error.WriteLineAsync($"shot failed scene {Pad}: {Url}"); return 6; }
}
return 0;
