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
    var O = await P.StandardOutput.ReadToEndAsync();
    var E = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    await Console.Error.WriteLineAsync("out=" + O.Trim() + " err=" + E.Trim());
}

await RunPs("Get-CimInstance Win32_Process | Where-Object { $_.Name -like 'tunnel-client*' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -like '*tunnel-client*') } | ForEach-Object { Write-Output $_.ProcessId; Stop-Process -Id $_.ProcessId -Force }");
return 0;
