#:property TargetFramework=net11.0
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/strip-wolfs-namespace.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Dirs = new List<string>();
foreach (Match M in StripWolfsNamespacePatterns.ConstString().Matches(Body)) { Dirs.Add(M.Groups[2].Value); }
if (Dirs.Count == 0) { await Console.Error.WriteLineAsync("specific must declare at least one const string Dir/WorkerDir"); return 3; }

var Touched = 0;
foreach (var Dir in Dirs)
{
    if (!Directory.Exists(Dir)) { continue; }
    foreach (var F in Directory.GetFiles(Dir, "*.cs", SearchOption.AllDirectories))
    {
        var Original = await File.ReadAllTextAsync(F);
        var Updated = Original
            .Replace("namespace Scripts", "namespace Scripts", StringComparison.Ordinal)
            .Replace("namespace CdpTool", "namespace CdpTool", StringComparison.Ordinal)
            .Replace("namespace Cdp", "namespace Cdp", StringComparison.Ordinal)
            .Replace("using Scripts;", "using Scripts;", StringComparison.Ordinal)
            .Replace("using CdpTool;", "using CdpTool;", StringComparison.Ordinal)
            .Replace("using Cdp;", "using Cdp;", StringComparison.Ordinal);
        if (Updated != Original) { await File.WriteAllTextAsync(F, Updated); Touched++; }
    }
}
if (Touched > 0) { await Console.Out.WriteLineAsync($"touched {Touched.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
return 0;

namespace Scripts
{
    internal static partial class StripWolfsNamespacePatterns
    {
        [GeneratedRegex("""const\s+string\s+(\w+)\s*=\s*@?"((?:[^"\\]|\\.)*)"\s*;""")]
        internal static partial Regex ConstString();
    }
}
