using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/patch-source.cs scripts/<patch-source-X>-config.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var SpecBody = await File.ReadAllTextAsync(SpecPath);
var Strs = PatchSourcePatterns.ConstString().Matches(SpecBody)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);

if (!Strs.TryGetValue("TargetFile", out var TargetFile)) { await Console.Error.WriteLineAsync("specific must declare const string TargetFile"); return 3; }
if (!File.Exists(TargetFile)) { await Console.Error.WriteLineAsync($"target not found: {TargetFile}"); return 4; }

var Body = await File.ReadAllTextAsync(TargetFile);
var Applied = 0;
var Missed = 0;
for (var I = 1; I <= 99; I++)
{
    var FindKey = $"Find_{I.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)}";
    var ReplaceKey = $"Replace_{I.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)}";
    if (!Strs.TryGetValue(FindKey, out var Find)) { break; }
    if (string.IsNullOrEmpty(Find) || string.Equals(Find, "___UNUSED_SLOT___", StringComparison.Ordinal)) { break; }
    if (!Strs.TryGetValue(ReplaceKey, out var Replace)) { await Console.Error.WriteLineAsync($"missing {ReplaceKey}"); return 5; }
    var FindUnescaped = Regex.Unescape(Find);
    var ReplaceUnescaped = Regex.Unescape(Replace);
    if (!Body.Contains(FindUnescaped, StringComparison.Ordinal))
    {
        if (Body.Contains(ReplaceUnescaped, StringComparison.Ordinal))
        {
            continue;
        }
        await Console.Error.WriteLineAsync($"  [miss] pair {I.ToString(System.Globalization.CultureInfo.InvariantCulture)} anchor not found");
        Missed++;
        continue;
    }
    Body = Body.Replace(FindUnescaped, ReplaceUnescaped);
    Applied++;
}

if (Applied > 0) { await File.WriteAllTextAsync(TargetFile, Body); }
return Missed == 0 ? 0 : 6;

namespace Scripts
{
    internal static partial class PatchSourcePatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
