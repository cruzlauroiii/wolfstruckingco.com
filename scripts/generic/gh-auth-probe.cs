#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

var Psi = new ProcessStartInfo("gh") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
Psi.ArgumentList.Add("auth");
Psi.ArgumentList.Add("status");
try
{
    using var P = Process.Start(Psi);
    if (P is null) { return 1; }
    var Out = await P.StandardOutput.ReadToEndAsync();
    var Err = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    await Console.Out.WriteAsync(Out);
    await Console.Error.WriteAsync(Err);
    return P.ExitCode;
}
catch (Exception E)
{
    await Console.Error.WriteLineAsync("gh-auth-probe: " + E.Message);
    return 2;
}
