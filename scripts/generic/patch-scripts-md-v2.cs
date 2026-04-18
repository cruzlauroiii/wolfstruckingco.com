#:property TargetFramework=net11.0
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/patch-scripts-md-v2.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Strings = PatchScriptsMdV2Patterns.ConstString().Matches(await File.ReadAllTextAsync(SpecPath))
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
foreach (var Required in new[] { "Path", "Anchor" })
{
    if (!Strings.ContainsKey(Required)) { await Console.Error.WriteLineAsync($"specific missing const string {Required}"); return 3; }
}
var Path = Strings["Path"];
var Anchor = Strings["Anchor"];

var Rows = new List<(string Marker, string Row)>();
var MarkerPrefix = "Marker_";
var RowPrefix = "Row_";
foreach (var Key in Strings.Keys.Where(K => K.StartsWith(MarkerPrefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal))
{
    var Suffix = Key[MarkerPrefix.Length..];
    var RowKey = RowPrefix + Suffix;
    if (!Strings.TryGetValue(RowKey, out var Row)) { await Console.Error.WriteLineAsync($"missing paired const: {RowKey}"); return 4; }
    Rows.Add((Strings[Key], Row));
}
if (Rows.Count == 0) { await Console.Error.WriteLineAsync("specific must declare at least one Marker_NN/Row_NN pair"); return 5; }

var Text = await File.ReadAllTextAsync(Path);
var AnchorIdx = Text.IndexOf(Anchor, StringComparison.Ordinal);
if (AnchorIdx < 0) { await Console.Error.WriteLineAsync($"anchor missing: {Anchor}"); return 6; }
var EolIdx = Text.IndexOf('\n', AnchorIdx, StringComparison.Ordinal);
if (EolIdx < 0) { await Console.Error.WriteLineAsync("no newline after anchor"); return 7; }

var Sb = new System.Text.StringBuilder();
Sb.Append(Text[..(EolIdx + 1)]);
var Added = 0;
foreach (var (Marker, Row) in Rows)
{
    if (Text.Contains(Marker, StringComparison.Ordinal)) { continue; }
    Sb.Append(Row).Append('\n');
    Added++;
}
Sb.Append(Text[(EolIdx + 1)..]);
if (Added == 0) { await Console.Out.WriteLineAsync("all rows already present"); return 0; }
await File.WriteAllTextAsync(Path, Sb.ToString());
await Console.Out.WriteLineAsync($"appended {Added.ToString(System.Globalization.CultureInfo.InvariantCulture)} rows to SCRIPTS.md");
return 0;

namespace Scripts
{
    internal static partial class PatchScriptsMdV2Patterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
