#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string? Get(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        bool Verbatim = After.StartsWith("@", StringComparison.Ordinal);
        if (Verbatim) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}

var ProcessName = Get("ProcessName") ?? "";
var LaunchPath = Get("LaunchPath") ?? "";
var FallbackPath = Get("FallbackPath") ?? "";
var WaitMs = int.Parse(Get("WaitMs") ?? "5000");

if (!string.IsNullOrEmpty(ProcessName))
{
    var Procs = Process.GetProcessesByName(ProcessName);
    foreach (var P in Procs) { try { P.Kill(true); } catch { } }
    await Task.Delay(2500);
}

string? UsePath = null;
if (!string.IsNullOrEmpty(LaunchPath) && File.Exists(LaunchPath)) UsePath = LaunchPath;
else if (!string.IsNullOrEmpty(FallbackPath) && File.Exists(FallbackPath)) UsePath = FallbackPath;

if (UsePath is null) { Console.WriteLine($"chrome path not found"); return 3; }

var Psi = new ProcessStartInfo(UsePath) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Maximized };
Psi.ArgumentList.Add("--start-maximized");
Psi.ArgumentList.Add("--remote-allow-origins=*");
Psi.ArgumentList.Add("--disable-features=SessionCrashedBubble,InfoBars");
Psi.ArgumentList.Add("--no-first-run");
Psi.ArgumentList.Add("--no-default-browser-check");
Psi.ArgumentList.Add("--hide-crash-restore-bubble");
Psi.ArgumentList.Add("--disable-session-crashed-bubble");
Psi.ArgumentList.Add("--restore-last-session=false");
Process.Start(Psi);
await Task.Delay(WaitMs);
return 0;
