#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false

using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/run-with-watchdog.cs scripts/<run-with-watchdog-X>-config.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strs = WatchdogPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
var Nums = WatchdogPatterns.ConstInt().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => int.Parse(M.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);

if (!Strs.TryGetValue("TargetCs", out var TargetCs)) { await Console.Error.WriteLineAsync("specific must declare const string TargetCs"); return 3; }
if (!Strs.TryGetValue("ErrorPattern", out var ErrorPattern)) { await Console.Error.WriteLineAsync("specific must declare const string ErrorPattern"); return 4; }
var WorkingDir = Strs.TryGetValue("WorkingDir", out var Wd) ? Wd : System.IO.Path.GetDirectoryName(TargetCs)!;
var StallSec = Nums.TryGetValue("StallSec", out var Ss) ? Ss : 5;

var Re = new Regex(ErrorPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
var Psi = new ProcessStartInfo("dotnet", $"run \"{TargetCs}\"")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = WorkingDir,
};
using var Proc = Process.Start(Psi)!;
var LastOutputUtc = DateTimeOffset.UtcNow;
var Triggered = false;
string? FirstHit = null;
string? StallReason = null;

async Task PumpAsync(System.IO.StreamReader Stream, bool IsErr)
{
    string? Line;
    while ((Line = await Stream.ReadLineAsync()) != null)
    {
        LastOutputUtc = DateTimeOffset.UtcNow;
        if (IsErr) { await Console.Error.WriteLineAsync(Line); }
        else { await Console.Out.WriteLineAsync(Line); }
        if (!Triggered && Re.IsMatch(Line))
        {
            Triggered = true;
            FirstHit = Line;
            await CaptureDiagnosticsAsync(WorkingDir, "regex-match");
            try { Proc.Kill(entireProcessTree: true); } catch { }
            return;
        }
    }
}

async Task StallTimerAsync()
{
    while (!Proc.HasExited && !Triggered)
    {
        await Task.Delay(1000);
        if ((DateTimeOffset.UtcNow - LastOutputUtc).TotalSeconds >= StallSec)
        {
            Triggered = true;
            StallReason = $"no output for {StallSec.ToString(System.Globalization.CultureInfo.InvariantCulture)}s";
            await Console.Error.WriteLineAsync($"\n!! watchdog STALL — {StallReason}");
            await CaptureDiagnosticsAsync(WorkingDir, "stall");
            try { Proc.Kill(entireProcessTree: true); } catch { }
            return;
        }
    }
}

async Task CaptureDiagnosticsAsync(string Wd, string Tag)
{
    try
    {
        var Stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var TmpDir = System.IO.Path.GetTempPath();
        var ShotPath = System.IO.Path.Combine(TmpDir, $"wolfs-watchdog-{Tag}-{Stamp}.png");
        var GridPath = ShotPath.Replace(".png", "-grid.png", StringComparison.Ordinal);
        await RunSubprocessAsync(Wd, "scripts/generic/chrome-devtools.cs", $"-- focus_chrome");
        await RunSubprocessAsync(Wd, "scripts/generic/chrome-devtools.cs", "-- screenshot_desktop");
        var DefaultShot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "screenshot-desktop.png");
        if (File.Exists(DefaultShot)) { File.Copy(DefaultShot, ShotPath, true); }
        var GridConfigPath = System.IO.Path.Combine(Wd, "scripts", "specific", "add-grid-scratch-config.cs");
        if (File.Exists(GridConfigPath))
        {
            var GridConfigBody = $"return 0;\r\n\r\nnamespace Scripts\r\n{{\r\n    internal static class AddGridScratchConfig\r\n    {{\r\n        public const string InputPath = @\"{ShotPath}\";\r\n        public const string OutputPath = @\"{GridPath}\";\r\n        public const int GridStep = 100;\r\n    }}\r\n}}\r\n";
            await File.WriteAllTextAsync(GridConfigPath, GridConfigBody);
            await RunSubprocessAsync(Wd, "scripts/generic/add-grid.cs", "scripts/specific/add-grid-scratch-config.cs");
        }
        var ConsoleOut = await RunSubprocessAsync(Wd, "scripts/generic/chrome-devtools.cs", "-- list_console_messages");
        var ConsolePath = System.IO.Path.Combine(TmpDir, $"wolfs-watchdog-{Tag}-{Stamp}-console.txt");
        await File.WriteAllTextAsync(ConsolePath, ConsoleOut);
        await Console.Error.WriteLineAsync($"!! diagnostics:");
        await Console.Error.WriteLineAsync($"!!   screenshot       {ShotPath}");
        await Console.Error.WriteLineAsync($"!!   screenshot+grid  {GridPath}");
        await Console.Error.WriteLineAsync($"!!   console messages {ConsolePath}");
    }
    catch (Exception Ex) { await Console.Error.WriteLineAsync($"!! diagnostic capture failed: {Ex.Message}"); }
}

async Task<string> RunSubprocessAsync(string Wd, string ScriptPath, string ExtraArgs)
{
    var Sub = new ProcessStartInfo("dotnet", $"run \"{System.IO.Path.Combine(Wd, ScriptPath)}\" {ExtraArgs}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Wd,
    };
    using var P = Process.Start(Sub)!;
    var Out = await P.StandardOutput.ReadToEndAsync();
    var Err = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    return Out + Err;
}

var OutTask = PumpAsync(Proc.StandardOutput, false);
var ErrTask = PumpAsync(Proc.StandardError, true);
var StallTask = StallTimerAsync();
await Task.WhenAll(OutTask, ErrTask, StallTask);
await Proc.WaitForExitAsync();

if (Triggered)
{
    if (FirstHit != null) { await Console.Error.WriteLineAsync($"\n!! watchdog tripped on regex match: {FirstHit}"); }
    if (StallReason != null) { await Console.Error.WriteLineAsync($"\n!! watchdog tripped on stall: {StallReason}"); }
    await Console.Error.WriteLineAsync($"!! killed {TargetCs}");
    return 6;
}
return Proc.ExitCode;

namespace Scripts
{
    internal static partial class WatchdogPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"const\s+int\s+(?<name>\w+)\s*=\s*(?<value>-?\d+)\s*;", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstInt();
    }
}
