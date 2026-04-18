using System.Text;
using System.Text.RegularExpressions;
using Scripts;

const string Md = @"C:\repo\public\wolfstruckingco.com\main\SCRIPTS.md";
var Lines = await File.ReadAllLinesAsync(Md);
var Sb = new StringBuilder();
foreach (var Line in Lines)
{
    var M = UpdateScriptsMdPatterns.Row().Match(Line);
    if (!M.Success) { Sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{Line}"); continue; }
    var Name = M.Groups["name"].Value;
    var Desc = M.Groups["desc"].Value;
    var IsSpecific = Desc.StartsWith("Specific", StringComparison.Ordinal) || Name.EndsWith("-config.cs", StringComparison.Ordinal);
    var Folder = IsSpecific ? "scripts/specific" : "scripts/generic";
    Sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"| `{Folder}/{Name}` | `{Folder}/` | {Desc} |");
}
await File.WriteAllTextAsync(Md, Sb.ToString());
return 0;

namespace Scripts
{
    internal static partial class UpdateScriptsMdPatterns
    {
        [GeneratedRegex(@"^\| `scripts/(?<name>[^`]+\.cs)` \| `scripts/` \| (?<desc>.+) \|$", RegexOptions.ExplicitCapture)]
        internal static partial Regex Row();
    }
}
