using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/grep-file.cs <specific.cs>"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }
var Consts = Patterns.ConstDecl().Matches(await File.ReadAllTextAsync(SpecPath))
    .Select(M => M.Groups["value"].Value)
    .ToList();
if (Consts.Count < 2) { await Console.Error.WriteLineAsync("specific needs at least Path + Pattern consts"); return 3; }
var Path = Consts[0];
var Pattern = Consts[1];
if (!File.Exists(Path)) { await Console.Error.WriteLineAsync($"target not found: {Path}"); return 4; }
var Re = new Regex(Pattern, RegexOptions.IgnoreCase);
var Lines = await File.ReadAllLinesAsync(Path);
for (var I = 0; I < Lines.Length; I++)
{
    if (Re.IsMatch(Lines[I])) { await Console.Out.WriteLineAsync($"{(I + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}\t{Lines[I]}"); }
}
return 0;

namespace Scripts
{
    internal static partial class Patterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstDecl();
    }
}
