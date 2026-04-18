#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// rename-namespace.cs — strip the WolfsTruckingCo prefix from every project,
// folder, csproj, namespace, and using directive so the codebase is generic.
//   src\SharedUI\SharedUI.csproj  →  src\SharedUI\SharedUI.csproj
//   namespace SharedUI.Pages   →  namespace SharedUI.Pages
//   using Domain.Models        →  using Domain.Models
//
//   dotnet run scripts/rename-namespace.cs                             # default repo
//   dotnet run scripts/rename-namespace.cs -- C:\…\main                # explicit repo
//   dotnet run scripts/rename-namespace.cs -- --dry                    # preview only
//
// Skips build outputs (bin/, obj/, docs/Generated/, docs/app/, publish/,
// wasm-publish/, wwwroot/app/) so generated artifacts aren't rewritten.

using System.Text.RegularExpressions;

var Repo = ResolveRoot(args);
var Dry = args.Contains("--dry");
Console.WriteLine($"repo = {Repo}{(Dry ? "  (dry-run)" : "")}");

var SkipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "bin", "obj", ".vs", ".git", "node_modules", "publish", "wasm-publish",
};
var SkipPathSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    Path.Combine("docs", "Generated"),
    Path.Combine("docs", "app"),
    Path.Combine("wwwroot", "app"),
};
var Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".cs", ".razor", ".csproj", ".slnx", ".slnf", ".md", ".props", ".targets",
    ".xaml", ".json", ".cshtml",
};
const string OldNs = "WolfsTruckingCo";

var Files = Directory.EnumerateFiles(Repo, "*.*", SearchOption.AllDirectories)
    .Where(P => !P.Split(Path.DirectorySeparatorChar).Any(Seg => SkipDirs.Contains(Seg)))
    .Where(P => !SkipPathSegments.Any(Seg => P.Contains(Seg, StringComparison.OrdinalIgnoreCase)))
    .Where(P => Extensions.Contains(Path.GetExtension(P)))
    .OrderBy(P => P)
    .ToList();

Console.WriteLine($"scanning {Files.Count} text file(s) for {OldNs}.* references…");
var TextHits = 0;
foreach (var F in Files)
{
    var Body = File.ReadAllText(F);
    if (!Body.Contains(OldNs))
    {
        continue;
    }
    var Updated = Regex.Replace(Body, @"WolfsTruckingCo\.", "");
    if (Updated == Body)
    {
        continue;
    }
    if (!Dry)
    {
        File.WriteAllText(F, Updated);
    }
    TextHits++;
    Console.WriteLine($"  ✎ {Path.GetRelativePath(Repo, F)}");
}
Console.WriteLine($"  → rewrote {TextHits} file(s)");
Console.WriteLine();

// Move folders/files: src\X\X.csproj  →  src\X\X.csproj
var SrcDir = Path.Combine(Repo, "src");
var Renames = new List<(string From, string To)>();
if (Directory.Exists(SrcDir))
{
    foreach (var Dir in Directory.GetDirectories(SrcDir, "*"))
    {
        var Name = Path.GetFileName(Dir);
        var NewName = Name[(OldNs.Length + 1)..];
        Renames.Add((Dir, Path.Combine(SrcDir, NewName)));
    }
}
// Top-level solution files
foreach (var F in Directory.GetFiles(Repo, "WolfsTruckingCo*.sl*", SearchOption.TopDirectoryOnly))
{
    var Name = Path.GetFileName(F);
    var NewName = Name.StartsWith(OldNs + ".", StringComparison.Ordinal)
        ? Name[(OldNs.Length + 1)..]
        : "App" + Name[OldNs.Length..];
    Renames.Add((F, Path.Combine(Repo, NewName)));
}

Console.WriteLine($"renaming {Renames.Count} directory/file path(s)…");
foreach (var (From, To) in Renames)
{
    Console.WriteLine($"  ↪ {Path.GetRelativePath(Repo, From)}  →  {Path.GetRelativePath(Repo, To)}");
    if (Dry)
    {
        continue;
    }
    if (Directory.Exists(From))
    {
        if (Directory.Exists(To))
        {
            Directory.Delete(To, recursive: true);
        }
        Directory.Move(From, To);
    }
    else if (File.Exists(From))
    {
        if (File.Exists(To))
        {
            File.Delete(To);
        }
        File.Move(From, To);
    }
}
Console.WriteLine();

// Pass 2: rename inner csproj files (e.g. src\SharedUI\SharedUI.csproj → src\SharedUI\SharedUI.csproj)
Console.WriteLine("renaming inner csproj files…");
var InnerRenames = 0;
foreach (var Dir in Directory.GetDirectories(SrcDir))
{
    foreach (var F in Directory.GetFiles(Dir, "*.csproj"))
    {
        var Name = Path.GetFileName(F);
        var NewPath = Path.Combine(Dir, Name[(OldNs.Length + 1)..]);
        Console.WriteLine($"  ↪ {Path.GetRelativePath(Repo, F)}  →  {Path.GetRelativePath(Repo, NewPath)}");
        if (!Dry)
        {
            File.Move(F, NewPath);
        }
        InnerRenames++;
    }
}
Console.WriteLine($"  → renamed {InnerRenames} csproj file(s)");
Console.WriteLine();

Console.WriteLine($"done. text-rewrites={TextHits}  path-renames={Renames.Count + InnerRenames}");
return 0;

static string ResolveRoot(string[] Args)
{
    foreach (var A in Args)
    {
        if (!A.StartsWith("--", StringComparison.Ordinal) && Directory.Exists(A))
        {
            return A;
        }
    }
    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
}
