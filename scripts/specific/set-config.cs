using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/set-config.cs scripts/<set-config-X>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var SpecBody = await File.ReadAllTextAsync(SpecPath);
var Strs = SetConfigPatterns.ConstString().Matches(SpecBody)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);

if (!Strs.TryGetValue("TargetConfig", out var TargetConfig)) { await Console.Error.WriteLineAsync("specific must declare const string TargetConfig"); return 3; }
if (!File.Exists(TargetConfig)) { await Console.Error.WriteLineAsync($"target not found: {TargetConfig}"); return 4; }

var TargetBody = await File.ReadAllTextAsync(TargetConfig);
var Touched = 0;
foreach (var (Name, NewValue) in Strs)
{
    if (Name == "TargetConfig") { continue; }
    if (!Name.StartsWith("Set_", StringComparison.Ordinal)) { continue; }
    var FieldName = Name[4..];
    var Anchor = SetConfigPatterns.FieldAnchor(FieldName);
    var Match = Anchor.Match(TargetBody);
    if (!Match.Success) { await Console.Error.WriteLineAsync($"field not found in target: {FieldName}"); continue; }
    var NewLine = $"{Match.Groups[\"prefix\"].Value}{Match.Groups[\"verb\"].Value}\"{NewValue.Replace("\"", "\\\"", StringComparison.Ordinal)}\";";
    TargetBody = TargetBody[..Match.Index] + NewLine + TargetBody[(Match.Index + Match.Length)..];
    Touched++;
}

if (Touched > 0) { await File.WriteAllTextAsync(TargetConfig, TargetBody); }
await Console.Out.WriteLineAsync($"set-config: {Touched.ToString(System.Globalization.CultureInfo.InvariantCulture)} field(s) updated in {TargetConfig}");
return 0;

namespace Scripts
{
    internal static partial class SetConfigPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        internal static Regex FieldAnchor(string FieldName) => new($"(?<prefix>\\s+public\\s+const\\s+string\\s+{Regex.Escape(FieldName)}\\s*=\\s*)(?<verb>@?)\"(?:[^\"\\\\]|\\\\.)*\"\\s*;");
    }
}
