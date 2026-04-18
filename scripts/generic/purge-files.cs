using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/purge-files.cs <specific.cs>"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }
var Body = await File.ReadAllTextAsync(SpecPath);

// The config explicitly lists every path to delete (one const string each).
// That explicit allowlist IS the safety mechanism — no extension filter needed.
var Paths = PurgePatterns.ConstString().Matches(Body).Select(M => M.Groups[1].Value).ToList();

foreach (var P in Paths)
{
    try { if (File.Exists(P)) { File.Delete(P); } }
    catch (IOException Ex) { await Console.Error.WriteLineAsync($"skip {P}: {Ex.Message}"); }
}

return 0;

namespace Scripts
{
    internal static partial class PurgePatterns
    {
        [GeneratedRegex("""const\s+string\s+\w+\s*=\s*@?"([^"]+)"\s*;""")]
        internal static partial Regex ConstString();
    }
}
