#:property TargetFramework=net11.0
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/patch-scripts-md.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Strings = PatchScriptsMdPatterns.ConstString().Matches(await File.ReadAllTextAsync(SpecPath))
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
foreach (var Required in new[] { "Path", "Anchor", "DuplicateMarker", "AddedRows" })
{
    if (!Strings.ContainsKey(Required)) { await Console.Error.WriteLineAsync($"specific missing const string {Required}"); return 3; }
}
var Path = Strings["Path"];
var Anchor = Strings["Anchor"];
var DuplicateMarker = Strings["DuplicateMarker"];
var AddedRows = Regex.Unescape(Strings["AddedRows"]);

var Text = await File.ReadAllTextAsync(Path);
if (Text.Contains(DuplicateMarker, StringComparison.Ordinal)) { await Console.Out.WriteLineAsync("already present, skipping"); return 0; }

var AnchorIdx = Text.IndexOf(Anchor, StringComparison.Ordinal);
if (AnchorIdx < 0) { await Console.Error.WriteLineAsync($"anchor not found: {Anchor}"); return 4; }
var EolIdx = Text.IndexOf('\n', AnchorIdx, StringComparison.Ordinal);
if (EolIdx < 0) { await Console.Error.WriteLineAsync("no newline after anchor"); return 5; }
var Updated = Text[..(EolIdx + 1)] + AddedRows + "\n" + Text[(EolIdx + 1)..];
await File.WriteAllTextAsync(Path, Updated);
await Console.Out.WriteLineAsync("wrote SCRIPTS.md with new rows");
return 0;

namespace Scripts
{
    internal static partial class PatchScriptsMdPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
