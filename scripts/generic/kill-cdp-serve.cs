#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

async Task RunPs(string Script)
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
    await Console.Error.WriteLineAsync("kill-cdp-serve: out=" + Out.Trim() + " err=" + Err.Trim());
}

await RunPs("Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'dotnet.exe' -and $_.CommandLine -like '*chrome-devtools.cs*' } | ForEach-Object { Write-Output $_.ProcessId; Stop-Process -Id $_.ProcessId -Force }");
return 0;
