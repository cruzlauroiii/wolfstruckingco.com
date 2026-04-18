#:property TargetFramework=net11.0
using System.Diagnostics;

var Minutes = args.Length > 0 && int.TryParse(args[0], System.Globalization.CultureInfo.InvariantCulture, out var M) ? M : 30;
var Filter = args.Length > 1 ? args[1] : null;
var Cutoff = DateTime.Now.AddMinutes(-Minutes);

foreach (var P in Process.GetProcesses().OrderBy(P => P.Id))
{
    try
    {
        var Name = P.ProcessName;
        if (Filter is not null && !Name.Contains(Filter, StringComparison.OrdinalIgnoreCase)) { continue; }
        if (Name is not "dotnet" and not "chrome" and not "ffmpeg" and not "edge-tts") { continue; }
        if (P.StartTime < Cutoff) { continue; }
        await Console.Out.WriteLineAsync($"{P.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),-7} {Name,-15} {P.StartTime.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)}");
    }
    catch (InvalidOperationException) { }
    catch (System.ComponentModel.Win32Exception) { }
}
return 0;
