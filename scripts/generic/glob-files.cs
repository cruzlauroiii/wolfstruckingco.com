using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/glob-files.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var ConstRe = GlobFilesPatterns.ConstString();
string? Pattern = null;
string? Root = null;
foreach (var (Name, Value) in ConstRe.Matches(await File.ReadAllTextAsync(SpecPath)).Select(M => (M.Groups["name"].Value, M.Groups["value"].Value)))
{
    if (Name == "Pattern") { Pattern = Value; }
    else if (Name == "Root") { Root = Value; }
}
if (Pattern is null || Root is null) { await Console.Error.WriteLineAsync("specific must declare const string Pattern and Root"); return 3; }
if (!Directory.Exists(Root)) { await Console.Error.WriteLineAsync($"root not found: {Root}"); return 4; }

foreach (var F in Directory.EnumerateFiles(Root, Pattern, SearchOption.AllDirectories))
{
    var Rel = Path.GetRelativePath(Root, F).Replace('\\', '/');
    await Console.Out.WriteLineAsync(Rel);
}
return 0;

namespace Scripts
{
    internal static partial class GlobFilesPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
