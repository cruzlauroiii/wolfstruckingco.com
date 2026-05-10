#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;
using System.Net.Http;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != '\u0022') continue;
        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var Wd = Get("Wd") ?? Environment.GetEnvironmentVariable("WOLFS_REPO") ?? Directory.GetCurrentDirectory();
var Generic = Get("Generic") ?? @"main\scripts\generic\chrome-devtools.cs";
var Config = Get("Config") ?? @"main\scripts\specific\chrome-devtools-serve-config.cs";
var Port = int.Parse(Get("Port") ?? "9334", CultureInfo.InvariantCulture);
var Match = Get("Match") ?? "chrome-devtools.cs";
var SkipHealth = Get("SkipHealth") == "true";

await Console.Error.WriteLineAsync("restart: host=" + Environment.MachineName + " cwd=" + Wd);

async Task<(int, string, string)> RunPs(string Script)
{
    var Psi = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -Command \u0022" + Script + "\u0022")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };
    var P = Process.Start(Psi)!;
    var Out = await P.StandardOutput.ReadToEndAsync();
    var Err = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    return (P.ExitCode, Out.Trim(), Err.Trim());
}

var KillScript = "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*" + Match + "*' } | ForEach-Object { Write-Output $_.ProcessId; Stop-Process -Id $_.ProcessId -Force }";
var (KillExit, KillOut, KillErr) = await RunPs(KillScript);
await Console.Error.WriteLineAsync("restart: kill exit=" + KillExit + " pids=[" + KillOut.Replace('\n', ',').Replace("\r", "") + "]");
if (!string.IsNullOrEmpty(KillErr)) await Console.Error.WriteLineAsync("restart: kill stderr=" + KillErr);

await Task.Delay(3000);

var Psi2 = new ProcessStartInfo("dotnet", "run " + Generic + " " + Config)
{
    WorkingDirectory = Wd,
    UseShellExecute = true,
    CreateNoWindow = false,
};
var Proc = Process.Start(Psi2);
if (Proc == null) { await Console.Error.WriteLineAsync("restart: spawn failed"); return 2; }
await Console.Error.WriteLineAsync("restart: spawned pid=" + Proc.Id);

if (SkipHealth) { return 0; }
using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
var ServeUrl = "http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture) + "/";
var Ok = false;
for (var I = 0; I < 30; I++)
{
    await Task.Delay(2000);
    try
    {
        using var Req = new HttpRequestMessage(HttpMethod.Post, new Uri(ServeUrl)) { Content = new StringContent("list_pages") };
        using var R = await Http.SendAsync(Req).ConfigureAwait(false);
        var Body = await R.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(Body) && !Body.StartsWith("ERR:", StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync("restart: serve up after " + ((I + 1) * 2).ToString(CultureInfo.InvariantCulture) + "s");
            Ok = true;
            break;
        }
    }
    catch { }
}
if (!Ok)
{
    await Console.Error.WriteLineAsync("restart: serve never came up");
    return 3;
}
return 0;
