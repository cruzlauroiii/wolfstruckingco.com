#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;
using System.Media;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

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

var Headline = Read("Headline") ?? "Wolfs pipeline needs you";
var Body = Read("Body") ?? "manual step required";
var AckPath = Read("AckPath") ?? Path.Combine(Path.GetTempPath(), "wolfs-alarm-ack.txt");
var BeepCount = ReadInt("BeepCount", 8);
var BeepFreq = ReadInt("BeepFreq", 880);
var BeepMs = ReadInt("BeepMs", 600);
var PollMs = ReadInt("PollMs", 2000);
var TimeoutSeconds = ReadInt("TimeoutSeconds", 1800);

if (File.Exists(AckPath)) File.Delete(AckPath);

async Task Beep()
{
    if (OperatingSystem.IsWindows())
    {
        for (var I = 0; I < BeepCount; I++)
        {
            try { Console.Beep(BeepFreq, BeepMs); } catch { }
            await Task.Delay(150);
        }
    }
    else
    {
        for (var I = 0; I < BeepCount; I++) { Console.Write("\a"); await Task.Delay(BeepMs); }
    }
}

void SystemTray()
{
    if (!OperatingSystem.IsWindows()) return;
    try
    {
        var Args = $"-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; $n = New-Object System.Windows.Forms.NotifyIcon; $n.Icon = [System.Drawing.SystemIcons]::Warning; $n.BalloonTipTitle = '{Headline.Replace(\"'\", \"`'\")}'; $n.BalloonTipText = '{Body.Replace(\"'\", \"`'\")}'; $n.Visible = $true; $n.ShowBalloonTip(15000); Start-Sleep -Seconds 16; $n.Dispose()\"";
        var Psi = new ProcessStartInfo("powershell", Args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        Process.Start(Psi);
    }
    catch { }
}

SystemTray();
var BeepTask = Beep();

var Sw = Stopwatch.StartNew();
while (Sw.Elapsed.TotalSeconds < TimeoutSeconds)
{
    if (File.Exists(AckPath))
    {
        var Content = (await File.ReadAllTextAsync(AckPath)).Trim();
        if (Content.Equals("ok", StringComparison.OrdinalIgnoreCase) || Content.Equals("done", StringComparison.OrdinalIgnoreCase) || Content.Equals("ack", StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(AckPath);
            return 0;
        }
    }
    await Task.Delay(PollMs);
}
return 3;
