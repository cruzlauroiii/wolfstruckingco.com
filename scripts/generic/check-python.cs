#:property TargetFramework=net11.0

using System.Diagnostics;

if (args.Length < 1) { return 1; }
var Spec = args[0];
if (!System.IO.File.Exists(Spec)) { return 2; }

try
{
    var Psi = new ProcessStartInfo("python", "--version")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    using var P = Process.Start(Psi)!;
    var Out = await P.StandardOutput.ReadToEndAsync() + await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    await Console.Out.WriteLineAsync(Out.Trim());
    return P.ExitCode;
}
catch (System.ComponentModel.Win32Exception)
{
    await Console.Error.WriteLineAsync("python not found in PATH");
    return 3;
}
