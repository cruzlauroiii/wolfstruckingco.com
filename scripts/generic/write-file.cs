using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var SpecBody = await File.ReadAllTextAsync(SpecPath);
var Strs = WriteFilePatterns.ConstString().Matches(SpecBody)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);

if (!Strs.TryGetValue("TargetFile", out var TargetFile)) { return 3; }
if (!Strs.TryGetValue("Content", out var Content)) { return 4; }
var Mode = Strs.TryGetValue("Mode", out var M2) ? M2.ToLowerInvariant() : "overwrite";

var Unescaped = Regex.Unescape(Content);
var Dir = Path.GetDirectoryName(TargetFile);
if (!string.IsNullOrEmpty(Dir) && !Directory.Exists(Dir)) { Directory.CreateDirectory(Dir); }

if (Mode == "append") { await File.AppendAllTextAsync(TargetFile, Unescaped); }
else { await File.WriteAllTextAsync(TargetFile, Unescaped); }
return 0;

namespace Scripts
{
    internal static partial class WriteFilePatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
