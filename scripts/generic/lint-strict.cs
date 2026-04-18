#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include ../specific/lint-strict-config.cs
using System.Reflection;
using System.Text.RegularExpressions;
using Scripts;

var Repo = ResolveRoot(args);
var WarnOnly = args.Contains("--warn");
var SrcRoot = Path.Combine(Repo, "src");
if (!Directory.Exists(SrcRoot))
{
    await Console.Error.WriteLineAsync($"src not found: {SrcRoot}");
    return 1;
}

var SkipDir = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "Generated", ".vs" };
var Files = Directory.EnumerateFiles(SrcRoot, "*.cs", SearchOption.AllDirectories)
    .Where(P => !P.Split(Path.DirectorySeparatorChar).Any(Seg => SkipDir.Contains(Seg))
        && !P.EndsWith(".razor.g.cs", StringComparison.Ordinal))
    .Order(StringComparer.Ordinal)
    .ToList();

Console.WriteLine($"linting {Files.Count} files under {SrcRoot}");
Console.WriteLine();

var Findings = new List<(string File, int Line, string Rule, string Detail)>();
foreach (var F in Files)
{
    var Body = await File.ReadAllTextAsync(F);
    var Lines = Body.Split('\n');
    LintMagicNumbers(F, Lines, Findings);
    LintMagicStrings(F, Lines, Findings);
    LintInterpolation(F, Lines, Findings);
    LintPascalCase(F, Lines, Findings);
}

var Grouped = Findings.GroupBy(F => F.Rule).OrderBy(G => G.Key);
foreach (var G in Grouped)
{
    Console.WriteLine($"━━ {G.Key}: {G.Count()} hit(s) ━━");
    foreach (var Hit in G.Take(20))
    {
        await Console.Out.WriteLineAsync($"  {Path.GetRelativePath(Repo, Hit.File)}:{Hit.Line.ToString(System.Globalization.CultureInfo.InvariantCulture)}  {Hit.Detail}");
    }
    if (G.Skip(20).Any())
    {
        await Console.Out.WriteLineAsync($"  … {(G.Count() - 20).ToString(System.Globalization.CultureInfo.InvariantCulture)} more");
    }
    await Console.Out.WriteLineAsync();
}

Console.WriteLine($"total: {Findings.Count} finding(s) across {Findings.Select(F => F.File).Distinct().Count()} file(s)");
return Findings.Count == 0 || WarnOnly ? 0 : 1;

static string ResolveRoot(string[] Args)
{
    for (var I = 0; I < Args.Length - 1; I++)
    {
        if (Args[I] == "--root")
        {
            return Args[I + 1];
        }
    }
    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
}

static void LintMagicNumbers(string File, string[] Lines, List<(string File, int Line, string Rule, string Detail)> Out)
{
    var Allowed = new HashSet<string>(StringComparer.Ordinal) { "0", "1", "-1", "2", "100", "1000" };
    var NumberRx = StrictPatterns.Number();
    for (var I = 0; I < Lines.Length; I++)
    {
        var L = Lines[I];
        if (IsCommentOrDirective(L) || L.Contains("[Parameter]") || L.Contains("const "))
        {
            continue;
        }
        foreach (Match M in NumberRx.Matches(L))
        {
            if (Allowed.Contains(M.Value))
            {
                continue;
            }

            var Before = L[..M.Index];
            if (Before.EndsWith("=\"", StringComparison.Ordinal) || Before.EndsWith("='", StringComparison.Ordinal) || Before.Contains("style=", StringComparison.Ordinal))
            {
                continue;
            }
            Out.Add((File, I + 1, "magic-number", $"{M.Value}  →  promote to a const"));
        }
    }
}

static void LintMagicStrings(string File, string[] Lines, List<(string File, int Line, string Rule, string Detail)> Out)
{
    var StringRx = StrictPatterns.QuotedString();
    for (var I = 0; I < Lines.Length; I++)
    {
        var L = Lines[I];
        if (IsCommentOrDirective(L) || L.Contains("const ") || L.Contains("[Attribute") || L.Contains("Description") || L.Contains("Title"))
        {
            continue;
        }
        foreach (var Body in StringRx.Matches(L).Select(M => M.Value))
        {
            if (L.TrimStart().StartsWith("using ", StringComparison.Ordinal))
            {
                continue;
            }
            if (Body.Length > 80)
            {
                continue;
            }
            Out.Add((File, I + 1, "magic-string", $"{Body}  →  promote to a const or resource"));
        }
    }
}

static void LintInterpolation(string File, string[] Lines, List<(string File, int Line, string Rule, string Detail)> Out)
{
    var DomainWords = typeof(LintStrictConfig)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(F => F.IsLiteral && F.Name.StartsWith("Word_", StringComparison.Ordinal))
        .OrderBy(F => F.Name, StringComparer.Ordinal)
        .Select(F => (string)F.GetRawConstantValue()!)
        .ToArray();
    var InterpRx = StrictPatterns.Interp();
    for (var I = 0; I < Lines.Length; I++)
    {
        var L = Lines[I];
        if (IsCommentOrDirective(L))
        {
            continue;
        }
        foreach (Match M in InterpRx.Matches(L))
        {
            var Hit = DomainWords.FirstOrDefault(W => M.Value.Contains(W, StringComparison.OrdinalIgnoreCase));
            if (Hit is null)
            {
                continue;
            }
            Out.Add((File, I + 1, "interpolation-domain-word", $"{Hit} inside $\"\"  →  pass as parameter"));
        }
    }
}

static void LintPascalCase(string File, string[] Lines, List<(string File, int Line, string Rule, string Detail)> Out)
{
    var DeclRx = StrictPatterns.Decl();
    var Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "args", "value", "sender", "e", "ex", "i", "j", "k",
    };
    for (var I = 0; I < Lines.Length; I++)
    {
        var L = Lines[I];
        if (IsCommentOrDirective(L))
        {
            continue;
        }
        foreach (Match M in DeclRx.Matches(L))
        {
            var Name = M.Groups[1].Value;
            if (Allowed.Contains(Name))
            {
                continue;
            }
            Out.Add((File, I + 1, "non-pascal-case", $"{Name}  →  PascalCase"));
        }
    }
}

static bool IsCommentOrDirective(string Line)
{
    var T = Line.TrimStart();
    return T.StartsWith("//", StringComparison.Ordinal)
        || T.StartsWith("/*", StringComparison.Ordinal)
        || T.StartsWith('*')
        || T.StartsWith("@*", StringComparison.Ordinal)
        || T.StartsWith('#');
}

namespace Scripts
{
    internal static partial class StrictPatterns
    {
        [GeneratedRegex(@"(?<![\w.])-?\d+(?:\.\d+)?(?![\w.])")]
        internal static partial Regex Number();

        [GeneratedRegex(@"""([^""\\]|\\.){3,}""")]
        internal static partial Regex QuotedString();

        [GeneratedRegex(@"\$@?""([^""\\]|\\.)*""")]
        internal static partial Regex Interp();

        [GeneratedRegex(@"\b(?:var|public|private|protected|internal|static)\s+(?:async\s+)?(?:[\w<>?,\s]+\s)?([a-z][A-Za-z0-9_]*)\s*[=({;]")]
        internal static partial Regex Decl();
    }
}

