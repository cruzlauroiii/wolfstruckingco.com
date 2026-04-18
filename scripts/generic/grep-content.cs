using System.Text;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var ConstRe = GrepContentPatterns.ConstString();
string? Pattern = null;
string? Root = null;
string? FilePattern = null;
string? OutputFile = null;
foreach (var (Name, Value) in ConstRe.Matches(await File.ReadAllTextAsync(SpecPath)).Select(M => (M.Groups["name"].Value, M.Groups["value"].Value)))
{
    if (Name == "Pattern") { Pattern = Value; }
    else if (Name == "Root") { Root = Value; }
    else if (Name == "FilePattern") { FilePattern = Value; }
    else if (Name == "OutputFile") { OutputFile = Value; }
}
if (Pattern is null || Root is null || FilePattern is null || OutputFile is null) { return 3; }
if (!Directory.Exists(Root)) { return 4; }

var Re = new Regex(Pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
var Sb = new StringBuilder();
foreach (var F in Directory.EnumerateFiles(Root, FilePattern, SearchOption.AllDirectories))
{
    var Rel = Path.GetRelativePath(Root, F).Replace('\\', '/');
    var Lines = await File.ReadAllLinesAsync(F);
    for (var I = 0; I < Lines.Length; I++)
    {
        if (Re.IsMatch(Lines[I])) { Sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"{Rel}:{I + 1}:{Lines[I]}\n"); }
    }
}
await File.WriteAllTextAsync(OutputFile, Sb.ToString());
return 0;

namespace Scripts
{
    internal static partial class GrepContentPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
