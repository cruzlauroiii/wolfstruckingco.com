#:property TargetFramework=net11.0

using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { return 1; }
var Spec = args[0];
if (!System.IO.File.Exists(Spec)) { return 2; }

var Body = await System.IO.File.ReadAllTextAsync(Spec);
var Re = PipInstallPatterns.PackageConst();
var M = Re.Match(Body);
if (!M.Success) { await Console.Error.WriteLineAsync("Package const required"); return 3; }
var Package = M.Groups["v"].Value;

var Psi = new ProcessStartInfo("python", "-m pip install " + Package + " --quiet")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync() + await P.StandardError.ReadToEndAsync();
await P.WaitForExitAsync();
if (P.ExitCode != 0) { await Console.Error.WriteLineAsync(Out.Trim()); }
return P.ExitCode;

namespace Scripts
{
    internal static partial class PipInstallPatterns
    {
        [GeneratedRegex("""const\s+string\s+Package\s*=\s*"(?<v>[^"]+)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex PackageConst();
    }
}
