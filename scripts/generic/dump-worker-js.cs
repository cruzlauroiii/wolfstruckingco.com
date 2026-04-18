#:property TargetFramework=net11.0

// dump-worker-js.cs - Specific. Emits the full worker.js body to stdout
// so it can be embedded as a const string in worker/worker.cs (item #7
// final: zero .js files at rest in the repo). Delegates to GENERIC
// dump-file.cs in config-driven mode.
using System.Diagnostics;
using Scripts;

var Psi = new ProcessStartInfo("dotnet", "run scripts/dump-file.cs scripts/dump-worker-js.cs")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = DumpWorkerJsConfig.Repo,
};
using var Proc = Process.Start(Psi)!;
await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
await Proc.WaitForExitAsync();
return Proc.ExitCode;

namespace Scripts
{
    internal static class DumpWorkerJsConfig
    {
        public const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
        public const string Path = @"worker\worker.js";
        public const string Mode = "full";
    }
}
