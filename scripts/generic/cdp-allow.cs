#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using Scripts;

if (args.Length < 1) { return 1; }
if (!System.IO.File.Exists(args[0])) { return 2; }

var Psi = new ProcessStartInfo("dotnet", $"run \"{Paths.Cdp}\" -- allow")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = Paths.Repo,
};
using var Proc = Process.Start(Psi)!;
await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
await Proc.WaitForExitAsync();
return Proc.ExitCode;
