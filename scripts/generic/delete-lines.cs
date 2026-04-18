using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/delete-lines.cs scripts/<delete-lines-X>-config.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var SpecBody = await File.ReadAllTextAsync(SpecPath);
var Strs = DeleteLinesPatterns.ConstString().Matches(SpecBody)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
var Ints = DeleteLinesPatterns.ConstInt().Matches(SpecBody)
    .ToDictionary(M => M.Groups["name"].Value, M => int.Parse(M.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);

if (!Strs.TryGetValue("TargetFile", out var TargetFile)) { await Console.Error.WriteLineAsync("specific must declare const string TargetFile"); return 3; }
if (!Ints.TryGetValue("StartLine", out var StartLine)) { await Console.Error.WriteLineAsync("specific must declare const int StartLine"); return 4; }
if (!Ints.TryGetValue("EndLine", out var EndLine)) { await Console.Error.WriteLineAsync("specific must declare const int EndLine"); return 5; }
if (!File.Exists(TargetFile)) { await Console.Error.WriteLineAsync($"target not found: {TargetFile}"); return 6; }
if (StartLine < 1 || EndLine < StartLine) { await Console.Error.WriteLineAsync($"invalid range {StartLine.ToString(System.Globalization.CultureInfo.InvariantCulture)}..{EndLine.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); return 7; }

var Lines = await File.ReadAllLinesAsync(TargetFile);
if (EndLine > Lines.Length) { await Console.Error.WriteLineAsync($"EndLine {EndLine.ToString(System.Globalization.CultureInfo.InvariantCulture)} exceeds file length {Lines.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); return 8; }

var Kept = Lines.Take(StartLine - 1).Concat(Lines.Skip(EndLine)).ToArray();
await File.WriteAllLinesAsync(TargetFile, Kept);
await Console.Out.WriteLineAsync($"delete-lines: removed {(EndLine - StartLine + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} lines ({StartLine.ToString(System.Globalization.CultureInfo.InvariantCulture)}..{EndLine.ToString(System.Globalization.CultureInfo.InvariantCulture)}) from {TargetFile}");
return 0;

namespace Scripts
{
    internal static partial class DeleteLinesPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
        [GeneratedRegex("""const\s+int\s+(?<name>\w+)\s*=\s*(?<value>-?\d+)\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstInt();
    }
}
