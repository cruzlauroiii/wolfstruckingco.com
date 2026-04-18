#:property TargetFramework=net11.0

using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { return 1; }
var Spec = args[0];
if (!System.IO.File.Exists(Spec)) { return 2; }

var Body = await System.IO.File.ReadAllTextAsync(Spec);
var Re = ListVoicesPatterns.OutputConst();
var M = Re.Match(Body);
if (!M.Success) { await Console.Error.WriteLineAsync("OutputFile const required"); return 3; }
var OutputFile = M.Groups["v"].Value;

var Psi = new ProcessStartInfo("python")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
Psi.ArgumentList.Add("-m");
Psi.ArgumentList.Add("edge_tts");
Psi.ArgumentList.Add("--list-voices");

using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync();
var Err = await P.StandardError.ReadToEndAsync();
await P.WaitForExitAsync();
if (P.ExitCode != 0) { await Console.Error.WriteLineAsync(Err.Trim()); return P.ExitCode; }
await System.IO.File.WriteAllTextAsync(OutputFile, Out);
return 0;

namespace Scripts
{
    internal static partial class ListVoicesPatterns
    {
        [GeneratedRegex("""const\s+string\s+OutputFile\s*=\s*@?"(?<v>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex OutputConst();
    }
}
