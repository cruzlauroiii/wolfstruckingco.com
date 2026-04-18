#:property TargetFramework=net11.0
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var ConstRe = BulkReplacePatterns.ConstString();
string? Root = null;
string? FilePattern = null;
string? Find = null;
string? Replace = null;
foreach (var (Name, Value) in ConstRe.Matches(await File.ReadAllTextAsync(SpecPath)).Select(M => (M.Groups["name"].Value, M.Groups["value"].Value)))
{
    if (Name == "Root") { Root = Value; }
    else if (Name == "FilePattern") { FilePattern = Value; }
    else if (Name == "Find") { Find = Regex.Unescape(Value); }
    else if (Name == "Replace") { Replace = Regex.Unescape(Value); }
}
if (Root is null || FilePattern is null || Find is null || Replace is null) { return 3; }
if (!Directory.Exists(Root)) { return 4; }

var Count = 0;
foreach (var F in Directory.EnumerateFiles(Root, FilePattern, SearchOption.AllDirectories))
{
    var Body = await File.ReadAllTextAsync(F);
    if (!Body.Contains(Find, StringComparison.Ordinal)) { continue; }
    var New = Body.Replace(Find, Replace, StringComparison.Ordinal);
    if (New == Body) { continue; }
    await File.WriteAllTextAsync(F, New);
    Count++;
}
await Console.Out.WriteLineAsync($"replaced in {Count.ToString(System.Globalization.CultureInfo.InvariantCulture)} files");
return 0;

namespace Scripts
{
    internal static partial class BulkReplacePatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
