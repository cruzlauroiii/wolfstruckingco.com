#:property TargetFramework=net11.0
using System.Text.RegularExpressions;
using Scripts;

var Repo = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var SharedUi = Path.Combine(Repo, "src", "SharedUI");
if (!Directory.Exists(SharedUi))
{
    await Console.Error.WriteLineAsync($"SharedUI not found: {SharedUi}");
    return 1;
}

var Map = new (string Old, string New)[]
{
    ("btn btn-primary",  "Btn"),
    ("btn btn-outline",  "Btn Ghost"),
    ("btn btn-google",   "SsoBtn"),
    ("btn btn-github",   "SsoBtn"),
    ("btn btn-azure",    "SsoBtn"),
    ("btn btn-okta",     "SsoBtn"),
    ("page-header",      "PageHeader"),
    ("page-subtitle",    "Sub"),
    ("card-title",       "CardTitle"),
    ("card-label",       "CardLabel"),
    ("card-value",       "Value"),
    ("stats-grid",       "Stats"),
    ("services-grid",    "Grid"),
    ("service-card",     "Card"),
    ("service-icon",     "ServiceIcon"),
    ("sso-grid",         "SsoButtons"),
    ("login-container",  "LoginWrap"),
    ("login-box",        "LoginCard"),
    ("login-brand",      "LoginBrand"),
    ("login-subtitle",   "LoginSubtitle"),
    ("login-divider",    "Divider"),
    ("hero",             "Hero"),
    ("map-container",    "MapWrap"),
    ("map-frame",        "MapFrame"),
    ("schedule-table",   "Table"),
    ("settings-form",    "Card"),
    ("form-group",       "Field"),
    ("brand-icon",       "Logo"),
    ("brand-text",       "BrandText"),
    ("brand",            "Brand"),
    ("nav-link",         "NavLink"),
    ("nav-section",      "NavSection"),
    ("card",             "Card"),
};

var ClassAttr = MigratePatterns.ClassAttr();
var FilesTouched = 0;
var Replacements = 0;

foreach (var File in Directory.EnumerateFiles(SharedUi, "*.razor", SearchOption.AllDirectories))
{
    var Original = await System.IO.File.ReadAllTextAsync(File);
    var Local = 0;
    var Updated = ClassAttr.Replace(Original, M =>
    {
        var Quote = M.Value.Contains('"') ? '"' : '\'';
        var Body = M.Groups["dq"].Success ? M.Groups["dq"].Value : M.Groups["sq"].Value;
        var Tokens = Body.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var Idx = 0;
        while (Idx < Tokens.Count - 1)
        {
            var Pair = Tokens[Idx] + " " + Tokens[Idx + 1];
            var Hit = Map.FirstOrDefault(X => X.Old.Contains(' ') && X.Old == Pair);
            if (Hit != default)
            {
                var Repl = Hit.New.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Tokens.RemoveRange(Idx, 2);
                Tokens.InsertRange(Idx, Repl);
                Local++;
                Idx = Math.Max(0, Idx - 1);
                continue;
            }
            Idx++;
        }
        for (var I = 0; I < Tokens.Count; I++)
        {
            var Hit = Map.FirstOrDefault(X => !X.Old.Contains(' ') && X.Old == Tokens[I]);
            if (Hit != default)
            {
                Tokens[I] = Hit.New;
                Local++;
            }
        }
        return $"class={Quote}{string.Join(' ', Tokens)}{Quote}";
    });
    if (Updated == Original)
    {
        continue;
    }
    await System.IO.File.WriteAllTextAsync(File, Updated);
    FilesTouched++;
    Replacements += Local;
    await Console.Out.WriteLineAsync($"  ✓ {Path.GetRelativePath(Repo, File)}  ({Local.ToString(System.Globalization.CultureInfo.InvariantCulture)} swaps)");
}

await Console.Out.WriteLineAsync();
await Console.Out.WriteLineAsync($"done — {FilesTouched.ToString(System.Globalization.CultureInfo.InvariantCulture)} files touched, {Replacements.ToString(System.Globalization.CultureInfo.InvariantCulture)} class-token swaps");
return 0;

namespace Scripts
{
    internal static partial class MigratePatterns
    {
        [GeneratedRegex("""class\s*=\s*"(?<dq>[^"]*)"|class\s*=\s*'(?<sq>[^']*)'""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ClassAttr();
    }
}
