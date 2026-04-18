#:property TargetFramework=net11.0

// inspect-login-html.cs - Specific. Owns the docs/Login/index.html path
// + grep pattern that proves the SSO pre-hydration snippet was rendered.
// Delegates to GENERIC dump-file.cs in config-driven mode.
using System.Diagnostics;
using Scripts;

var Psi = new ProcessStartInfo("dotnet", "run scripts/dump-file.cs scripts/inspect-login-html.cs")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = InspectLoginHtmlConfig.Repo,
};
using var Proc = Process.Start(Psi)!;
await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
await Proc.WaitForExitAsync();
return Proc.ExitCode;

namespace Scripts
{
    internal static class InspectLoginHtmlConfig
    {
        public const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
        public const string Path = @"docs\Login\index.html";
        public const string Mode = "grep";
        public const string Pattern = "db.js|wolfs-interop-shim|theme.js|demo.js|wolfs_session";
    }
}
