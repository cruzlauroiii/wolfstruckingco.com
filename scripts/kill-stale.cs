#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// kill-stale.cs — kill stale dotnet/chrome processes started in the last N
// minutes so publish/build can run cleanly. Skips the current dotnet host.
//
//   dotnet run scripts/kill-stale.cs                  # default 20 min
//   dotnet run scripts/kill-stale.cs -- 10 chrome     # 10 min, chrome only

using System.Diagnostics;

var Minutes = args.Length > 0 && int.TryParse(args[0], out var M) ? M : 20;
var Filter = args.Length > 1 ? args[1] : null;
var Cutoff = DateTime.Now.AddMinutes(-Minutes);
var Self = Environment.ProcessId;
var Killed = 0;
var Failed = 0;

foreach (var P in Process.GetProcesses())
{
    try
    {
        if (P.Id == Self)
        {
            continue;
        }
        var Name = P.ProcessName;
        if (Filter is not null && !Name.Contains(Filter, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }
        if (Name is not "dotnet" and not "chrome" and not "ffmpeg")
        {
            continue;
        }
        if (P.StartTime < Cutoff)
        {
            continue;
        }
        P.Kill();
        Killed++;
    }
    catch
    {
        Failed++;
    }
}
Console.WriteLine($"killed {Killed} processes, {Failed} skipped (already exited / access denied)");
return 0;
