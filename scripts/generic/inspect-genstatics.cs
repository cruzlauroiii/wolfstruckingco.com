#:property TargetFramework=net11.0

// inspect-genstatics.cs - Specific. Owns the path + grep pattern that
// proves the SSO pre-hydration snippet is in the generate-statics.cs
// template. Delegates to GENERIC dump-file.cs in config-driven mode.
using System.Diagnostics;
using Scripts;

var Psi = new ProcessStartInfo("dotnet", "run scripts/dump-file.cs scripts/inspect-genstatics.cs")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = InspectGenStaticsConfig.Repo,
};
using var Proc = Process.Start(Psi)!;
await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
await Proc.WaitForExitAsync();
return Proc.ExitCode;

namespace Scripts
{
    internal static class InspectGenStaticsConfig
    {
        public const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
        public const string Path = @"scripts\generate-statics.cs";
        public const string Mode = "grep";
        public const string Pattern = "wolfs_session|location.search.match|sso=";
    }
}
