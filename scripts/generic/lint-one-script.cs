using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/lint-one-script.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var ConstRe = LintOnePatterns.ConstString();
string? Target = null;
string? Repo = null;
foreach (var (Name, Value) in ConstRe.Matches(await File.ReadAllTextAsync(SpecPath)).Select(M => (M.Groups["name"].Value, M.Groups["value"].Value)))
{
    if (Name == "Target") { Target = Value; }
    else if (Name == "Repo") { Repo = Value; }
}
if (Target is null || Repo is null) { await Console.Error.WriteLineAsync("specific must declare const string Target and Repo"); return 3; }
if (!File.Exists(Target)) { await Console.Error.WriteLineAsync($"target not found: {Target}"); return 4; }

var Psi = new ProcessStartInfo("dotnet", $"build \"{Target}\" -nologo -v q")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = Repo,
};
using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync() + await P.StandardError.ReadToEndAsync();
await P.WaitForExitAsync();
if (P.ExitCode == 0) { await Console.Out.WriteLineAsync("OK"); return 0; }
foreach (var L in Out.Split('\n').Where(L => L.Contains(": error ", StringComparison.Ordinal)))
{
    await Console.Out.WriteLineAsync(L.Trim());
}
return 1;

namespace Scripts
{
    internal static partial class LintOnePatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
