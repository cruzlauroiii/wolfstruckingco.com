#:property TargetFramework=net11.0

using System.Diagnostics;

if (args.Length < 1) { return 1; }
if (!System.IO.File.Exists(args[0])) { return 2; }

var Psi = new ProcessStartInfo("git", "ls-files docs/Chat/")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = @"C:\repo\public\wolfstruckingco.com\main",
};
using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync();
await P.WaitForExitAsync();
await Console.Out.WriteAsync(Out);
return P.ExitCode;
