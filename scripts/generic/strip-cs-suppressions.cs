#:property TargetFramework=net11.0
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/strip-cs-suppressions.cs <specific.cs>"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Dirs = new List<string>();
Dirs.AddRange(StripPatterns.ConstString().Matches(await File.ReadAllTextAsync(SpecPath)).Select(M => M.Groups[2].Value));

string[] Prefixes =
[
    "#:property RunAnalyzersDuringBuild=",
    "#:property TreatWarningsAsErrors=",
    "#:property EnforceCodeStyleInBuild=",
    "#:property EnableNETAnalyzers=",
    "#:property NoWarn=",
    "#pragma warning disable",
    "#pragma warning restore",
];

var Touched = 0;
foreach (var D in Dirs)
{
    if (!Directory.Exists(D)) { continue; }
    foreach (var F in Directory.GetFiles(D, "*.cs", SearchOption.AllDirectories))
    {
        var Lines = await File.ReadAllLinesAsync(F);
        var Kept = new List<string>(Lines.Length);
        var Changed = false;
        foreach (var L in Lines)
        {
            var IsSuppression = Prefixes.Any(P => L.TrimStart().StartsWith(P, StringComparison.Ordinal));
            if (IsSuppression) { Changed = true; continue; }
            Kept.Add(L);
        }
        if (Changed) { await File.WriteAllLinesAsync(F, Kept); Touched++; }
    }
}
if (Touched > 0) { await Console.Out.WriteLineAsync($"stripped {Touched.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
return 0;

namespace Scripts
{
    internal static partial class StripPatterns
    {
        [GeneratedRegex("""const\s+string\s+(\w+)\s*=\s*@?"((?:[^"\\]|\\.)*)"\s*;""")]
        internal static partial Regex ConstString();
    }
}
