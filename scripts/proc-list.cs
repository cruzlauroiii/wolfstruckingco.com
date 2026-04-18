#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// proc-list.cs — list dotnet/chrome processes started in the last N minutes.
// Replaces inline `Get-Process | Where StartTime -gt …`.
//
//   dotnet run scripts/proc-list.cs                     # default: 30 min
//   dotnet run scripts/proc-list.cs -- 15 dotnet        # 15 min, dotnet only

using System.Diagnostics;

var Minutes = args.Length > 0 && int.TryParse(args[0], out var M) ? M : 30;
var Filter = args.Length > 1 ? args[1] : null;
var Cutoff = DateTime.Now.AddMinutes(-Minutes);

Console.WriteLine($"processes started after {Cutoff:HH:mm:ss}{(Filter is null ? "" : "  filter=" + Filter)}");
Console.WriteLine();
Console.WriteLine($"{"PID",-7} {"Name",-15} {"Started",-20} CmdLine");
foreach (var P in Process.GetProcesses().OrderBy(P => P.Id))
{
    try
    {
        var Name = P.ProcessName;
        if (Filter is not null && !Name.Contains(Filter, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }
        if (Name is not "dotnet" and not "chrome" and not "ffmpeg" and not "edge-tts")
        {
            continue;
        }
        if (P.StartTime < Cutoff)
        {
            continue;
        }
        Console.WriteLine($"{P.Id,-7} {Name,-15} {P.StartTime:HH:mm:ss}");
    }
    catch
    {
        // process exited, access denied, etc.
    }
}
return 0;
