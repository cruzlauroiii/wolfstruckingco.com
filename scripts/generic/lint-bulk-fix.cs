using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/lint-bulk-fix.cs <specific.cs>"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Dir = BulkPatterns.ConstString().Matches(await File.ReadAllTextAsync(SpecPath))
    .Where(M => M.Groups["name"].Value.Equals("Dir", StringComparison.Ordinal))
    .Select(M => M.Groups["value"].Value)
    .FirstOrDefault();
if (Dir is null || !Directory.Exists(Dir)) { await Console.Error.WriteLineAsync("specific must declare const string Dir"); return 3; }

var EmptyStringRe = BulkPatterns.EmptyString();
var Touched = 0;
foreach (var F in Directory.GetFiles(Dir, "*.cs", SearchOption.TopDirectoryOnly))
{
    if (Path.GetFileName(F).Equals("lint-bulk-fix.cs", StringComparison.OrdinalIgnoreCase)) { continue; }
    var Body = await File.ReadAllTextAsync(F);
    var Original = Body;
    Body = EmptyStringRe.Replace(Body, "$1string.Empty$2");

    var Lines = Body.Split('\n').ToList();
    var I = 1;
    while (I < Lines.Count)
    {
        var Curr = Lines[I].TrimStart();
        var Prev = Lines[I - 1].TrimEnd();
        if (Curr.StartsWith("// ", StringComparison.Ordinal)
            && Prev.Length != 0
            && !Prev.TrimStart().StartsWith("//", StringComparison.Ordinal))
        {
            Lines.Insert(I, string.Empty);
            I += 2;
            continue;
        }
        I++;
    }

    for (var J = Lines.Count - 1; J > 0; J--)
    {
        if (Lines[J].Trim().Length != 0) { continue; }
        var Prev = Lines[J - 1].TrimStart();
        if (!Prev.StartsWith("// ", StringComparison.Ordinal)) { continue; }
        var Top = J - 1;
        while (Top > 0 && Lines[Top - 1].TrimStart().StartsWith("// ", StringComparison.Ordinal)) { Top--; }
        if (Top == 0) { continue; }
        var BeforeBlock = Lines[Top - 1].TrimEnd();
        if (BeforeBlock.Length == 0) { continue; }
        Lines.RemoveAt(J);
    }
    Body = string.Join("\n", Lines);

    if (Body != Original) { await File.WriteAllTextAsync(F, Body); Touched++; }
}
if (Touched > 0) { await Console.Out.WriteLineAsync($"touched {Touched.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
return 0;

namespace Scripts
{
    internal static partial class BulkPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex("(\\W)\"\"(\\W)")]
        internal static partial Regex EmptyString();
    }
}
