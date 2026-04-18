using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Scripts;

const string Generic = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic";
const string Specific = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific";

var GenericNames = Directory.GetFiles(Generic, "*.cs").Select(Path.GetFileName).ToHashSet();
var SpecificNames = Directory.GetFiles(Specific, "*.cs").Select(Path.GetFileName).ToHashSet();

foreach (var Folder in new[] { Generic, Specific })
{
    foreach (var File1 in Directory.GetFiles(Folder, "*.cs"))
    {
        var Body = await File.ReadAllTextAsync(File1);
        var Sb = new StringBuilder();
        var Changed = false;
        foreach (var Line in Body.Split('\n'))
        {
            var M = FixIncludesPatterns.IncludeLine().Match(Line);
            if (!M.Success) { Sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"{Line}\n"); continue; }
            var Inc = M.Groups["name"].Value.Trim();
            if (Inc.Contains('/', StringComparison.Ordinal) || Inc.Contains('\\', StringComparison.Ordinal)) { Sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"{Line}\n"); continue; }
            string? Rel = null;
            if (GenericNames.Contains(Inc)) { Rel = Folder == Generic ? Inc : $"../generic/{Inc}"; }
            else if (SpecificNames.Contains(Inc)) { Rel = Folder == Specific ? Inc : $"../specific/{Inc}"; }
            if (Rel == null || Rel == Inc) { Sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"{Line}\n"); continue; }
            Sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"#:include {Rel}\n");
            Changed = true;
        }
        if (Changed)
        {
            var Out = Sb.ToString();
            if (Out.EndsWith('\n') && !Body.EndsWith('\n')) { Out = Out[..^1]; }
            await File.WriteAllTextAsync(File1, Out);
        }
    }
}
return 0;

namespace Scripts
{
    internal static partial class FixIncludesPatterns
    {
        [GeneratedRegex(@"^#:include\s+(?<name>[^\r\n]+\.cs)\s*$", RegexOptions.ExplicitCapture)]
        internal static partial Regex IncludeLine();
    }
}
