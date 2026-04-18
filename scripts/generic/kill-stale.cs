#:property TargetFramework=net11.0
using System.Diagnostics;

var Minutes = args.Length > 0 && int.TryParse(args[0], System.Globalization.CultureInfo.InvariantCulture, out var M) ? M : 20;
var Filter = args.Length > 1 ? args[1] : null;
var Cutoff = DateTime.Now.AddMinutes(-Minutes);
var Self = Environment.ProcessId;
var Killed = 0;

foreach (var P in Process.GetProcesses())
{
    try
    {
        if (P.Id == Self) { continue; }
        var Name = P.ProcessName;
        if (Filter is not null && !Name.Contains(Filter, StringComparison.OrdinalIgnoreCase)) { continue; }
        if (Name is not "dotnet" and not "chrome" and not "ffmpeg") { continue; }
        if (P.StartTime < Cutoff) { continue; }
        P.Kill();
        Killed++;
    }
    catch (InvalidOperationException) { }
    catch (System.ComponentModel.Win32Exception) { }
}
if (Killed > 0) { await Console.Out.WriteLineAsync($"killed {Killed.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
return 0;
