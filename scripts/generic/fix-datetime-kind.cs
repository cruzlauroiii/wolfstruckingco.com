#:property TargetFramework=net11.0
using System.Text.RegularExpressions;
using Scripts;

var Repo = args.FirstOrDefault(Directory.Exists)
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var Src = Path.Combine(Repo, "src");
if (!Directory.Exists(Src)) { await Console.Error.WriteLineAsync($"src not found: {Src}"); return 1; }

var Files = Directory.EnumerateFiles(Src, "*.*", SearchOption.AllDirectories)
    .Where(P => (P.EndsWith(".cs", StringComparison.Ordinal) || P.EndsWith(".razor", StringComparison.Ordinal))
             && !P.Split(Path.DirectorySeparatorChar).Any(S => S is "bin" or "obj"))
    .ToList();

var DateRx = DateTimeKindPatterns.NewDateTime();
var Touched = 0;
foreach (var F in Files)
{
    var Body = await File.ReadAllTextAsync(F);
    var Local = 0;
    var Updated = DateRx.Replace(Body, M =>
    {
        var ArgList = M.Groups["args"].Value;
        if (ArgList.Contains("DateTimeKind", StringComparison.Ordinal)) { return M.Value; }
        Local++;
        return $"new DateTime({ArgList}, DateTimeKind.Local)";
    });
    if (Local > 0) { await File.WriteAllTextAsync(F, Updated); Touched++; }
}

if (Touched > 0) { await Console.Out.WriteLineAsync($"touched {Touched.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
return 0;

namespace Scripts
{
    internal static partial class DateTimeKindPatterns
    {
        [GeneratedRegex(@"new DateTime\((?<args>[^()]+)\)", RegexOptions.ExplicitCapture)]
        internal static partial Regex NewDateTime();
    }
}
