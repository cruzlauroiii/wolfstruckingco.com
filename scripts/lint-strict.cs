#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// lint-strict.cs — single-pass strict linter for the SharedUI codebase.
// Flags four categories so generic code stays generic:
//   1. Magic numbers   — numeric literals other than -1/0/1/2/100, outside attribute-args & enum members.
//   2. Magic strings   — non-empty string literals >2 chars outside [Attribute], const, or # directives.
//   3. PascalCase      — local-var, parameter, method, property, type names that don't start uppercase.
//   4. Interpolation   — $"…" expressions whose static text contains domain words (Wolfs, Cloudflare, …).
//
//   dotnet run scripts/lint-strict.cs                            # walk src/, fail on any hit
//   dotnet run scripts/lint-strict.cs -- --root C:\…\main         # explicit repo
//   dotnet run scripts/lint-strict.cs -- --warn                  # report but exit 0
//
// File-based dotnet 11 program. Pure regex — no Roslyn dependency, so the script is
// portable, fast, and stays under 300 lines. Run from PowerShell tool only.

using System.Text.RegularExpressions;

var Repo = ResolveRoot(args);
var WarnOnly = args.Contains("--warn");
var SrcRoot = Path.Combine(Repo, "src");
if (!Directory.Exists(SrcRoot))
{
    Console.Error.WriteLine($"src not found: {SrcRoot}");
    return 1;
}

var SkipDir = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "Generated", ".vs" };
var Files = Directory.EnumerateFiles(SrcRoot, "*.cs", SearchOption.AllDirectories)
    .Where(P => !P.Split(Path.DirectorySeparatorChar).Any(Seg => SkipDir.Contains(Seg)))
    .Where(P => !P.EndsWith(".razor.g.cs", StringComparison.Ordinal))
    .OrderBy(P => P)
    .ToList();

Console.WriteLine($"linting {Files.Count} files under {SrcRoot}");
Console.WriteLine();

var Findings = new List<(string File, int Line, string Rule, string Detail)>();
foreach (var F in Files)
{
    var Body = File.ReadAllText(F);
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
        Console.WriteLine($"  {Path.GetRelativePath(Repo, Hit.File)}:{Hit.Line}  {Hit.Detail}");
    }
    if (G.Count() > 20)
    {
        Console.WriteLine($"  … {G.Count() - 20} more");
    }
    Console.WriteLine();
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
    var NumberRx = new Regex(@"(?<![\w.])-?\d+(?:\.\d+)?(?![\w.])");
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
            // Skip CSS hex inside style="..." or HTML attribute values like Height="600".
            var Before = L[..M.Index];
            if (Before.EndsWith("=\"") || Before.EndsWith("='") || Before.Contains("style="))
            {
                continue;
            }
            Out.Add((File, I + 1, "magic-number", $"{M.Value}  →  promote to a const"));
        }
    }
}

static void LintMagicStrings(string File, string[] Lines, List<(string File, int Line, string Rule, string Detail)> Out)
{
    var StringRx = new Regex(@"""([^""\\]|\\.){3,}""");
    for (var I = 0; I < Lines.Length; I++)
    {
        var L = Lines[I];
        if (IsCommentOrDirective(L) || L.Contains("const ") || L.Contains("[Attribute") || L.Contains("Description") || L.Contains("Title"))
        {
            continue;
        }
        foreach (Match M in StringRx.Matches(L))
        {
            var Body = M.Value;
            // Allow Razor markup (mostly attribute values like class="Card") and using directives.
            if (L.TrimStart().StartsWith("using ", StringComparison.Ordinal))
            {
                continue;
            }
            if (Body.Length > 80)
            {
                continue;  // long markup or SQL is not a magic string by this rule.
            }
            Out.Add((File, I + 1, "magic-string", $"{Body}  →  promote to a const or resource"));
        }
    }
}

static void LintInterpolation(string File, string[] Lines, List<(string File, int Line, string Rule, string Detail)> Out)
{
    var DomainWords = new[] { "Wolfs", "wolfstruckingco", "Cloudflare", "Valhalla", "edge-tts", "TWIC", "CDL" };
    var InterpRx = new Regex(@"\$@?""([^""\\]|\\.)*""");
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
    // Catches:  var foo = …;  / private string foo;  / public int foo() { … }
    var DeclRx = new Regex(@"\b(?:var|public|private|protected|internal|static)\s+(?:async\s+)?(?:[\w<>?,\s]+\s)?([a-z][A-Za-z0-9_]*)\s*[=({;]");
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
        || T.StartsWith("*", StringComparison.Ordinal)
        || T.StartsWith("@*", StringComparison.Ordinal)
        || T.StartsWith("#", StringComparison.Ordinal);
}
