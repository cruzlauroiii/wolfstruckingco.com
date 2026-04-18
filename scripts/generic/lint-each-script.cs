using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/lint-each-script.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var ConstRe = LintEachPatterns.ConstString();
string? Dir = null;
string? Repo = null;
var SpecText = await File.ReadAllTextAsync(SpecPath);
foreach (var (Name, Value) in ConstRe.Matches(SpecText).Select(M => (M.Groups["name"].Value, M.Groups["value"].Value)))
{
    if (Name == "Dir") { Dir = Value; }
    else if (Name == "Repo") { Repo = Value; }
}
if (Dir is null || Repo is null) { await Console.Error.WriteLineAsync("specific must declare const string Dir and Repo"); return 3; }

var IncludeRe = LintEachPatterns.IncludeDirective();
var IncludedByOthers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var F in Directory.GetFiles(Dir, "*.cs", SearchOption.TopDirectoryOnly))
{
    var Text = await File.ReadAllTextAsync(F);
    foreach (Match M in IncludeRe.Matches(Text)) { IncludedByOthers.Add(M.Groups["file"].Value); }
}

var Files = Directory.GetFiles(Dir, "*.cs", SearchOption.TopDirectoryOnly);
Array.Sort(Files, StringComparer.Ordinal);
var Failed = 0;
var Linted = 0;
foreach (var F in Files)
{
    if (IncludedByOthers.Contains(Path.GetFileName(F))) { continue; }
    if (Path.GetFileName(F).Equals("lint-each-script.cs", StringComparison.Ordinal)) { continue; }
    Linted++;
    var Rel = Path.GetRelativePath(Repo, F).Replace('\\', '/');
    var Psi = new ProcessStartInfo("dotnet", $"build \"{F}\" -nologo -v q")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Repo,
    };
    using var P = Process.Start(Psi)!;
    var Out = await P.StandardOutput.ReadToEndAsync() + await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    if (P.ExitCode != 0)
    {
        Failed++;
        Console.WriteLine($"FAIL {Rel}");
        foreach (var L in Out.Split('\n').Where(L => L.Contains(": error ", StringComparison.Ordinal)).Take(3)) { Console.WriteLine($"  {L.Trim()}"); }
    }
}
Console.WriteLine($"--- {Failed.ToString(System.Globalization.CultureInfo.InvariantCulture)} of {Linted.ToString(System.Globalization.CultureInfo.InvariantCulture)} ---");
return Failed == 0 ? 0 : 1;

namespace Scripts
{
    internal static partial class LintEachPatterns
    {
        [GeneratedRegex(@"^\s*#:include\s+(?<file>[\w\-.]+\.cs)\s*$", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex IncludeDirective();

        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
