#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include ../specific/rename-namespace-config.cs
using System.Text.RegularExpressions;
using Scripts;

var Repo = ResolveRoot(args);
var Dry = args.Contains("--dry");

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
var OldNs = RenameNamespaceConfig.OldNs;

var Files = Directory.EnumerateFiles(Repo, "*.*", SearchOption.AllDirectories)
    .Where(P => !P.Split(Path.DirectorySeparatorChar).Any(Seg => SkipDirs.Contains(Seg))
             && !SkipPathSegments.Any(Seg => P.Contains(Seg, StringComparison.OrdinalIgnoreCase))
             && Extensions.Contains(Path.GetExtension(P)))
    .Order(StringComparer.Ordinal)
    .ToList();

var ReplacerRe = new Regex(Regex.Escape(OldNs) + @"\.");
var TextHits = 0;
foreach (var F in Files)
{
    var Body = await File.ReadAllTextAsync(F);
    if (!Body.Contains(OldNs, StringComparison.Ordinal)) { continue; }
    var Updated = ReplacerRe.Replace(Body, string.Empty);
    if (Updated == Body) { continue; }
    if (!Dry) { await File.WriteAllTextAsync(F, Updated); }
    TextHits++;
}

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

foreach (var F in Directory.GetFiles(Repo, RenameNamespaceConfig.SlnPattern, SearchOption.TopDirectoryOnly))
{
    var Ext = Path.GetExtension(F);
    var NewName = string.Concat(RenameNamespaceConfig.NewSlnPrefix, Ext);
    Renames.Add((F, Path.Combine(Repo, NewName)));
}

foreach (var (From, To) in Renames)
{
    if (Dry) { continue; }
    if (Directory.Exists(From))
    {
        if (Directory.Exists(To)) { Directory.Delete(To, recursive: true); }
        Directory.Move(From, To);
    }
    else if (File.Exists(From))
    {
        if (File.Exists(To)) { File.Delete(To); }
        File.Move(From, To);
    }
}

var InnerRenames = 0;
foreach (var Dir in Directory.GetDirectories(SrcDir))
{
    foreach (var F in Directory.GetFiles(Dir, "*.csproj"))
    {
        var Name = Path.GetFileName(F);
        var NewPath = Path.Combine(Dir, Name[(OldNs.Length + 1)..]);
        if (!Dry) { File.Move(F, NewPath); }
        InnerRenames++;
    }
}

await Console.Out.WriteLineAsync($"text={TextHits.ToString(System.Globalization.CultureInfo.InvariantCulture)} paths={(Renames.Count + InnerRenames).ToString(System.Globalization.CultureInfo.InvariantCulture)}");
return 0;

static string ResolveRoot(string[] Args)
{
    foreach (var A in Args)
    {
        if (!A.StartsWith("--", StringComparison.Ordinal) && Directory.Exists(A)) { return A; }
    }
    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
}

