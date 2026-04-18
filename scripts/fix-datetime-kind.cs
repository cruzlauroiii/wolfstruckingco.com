#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// fix-datetime-kind.cs — append `, DateTimeKind.Local` to every `new DateTime(...)`
// constructor that doesn't already specify a kind. Resolves Sonar S6562 across the
// codebase in one pass.
//
//   dotnet run scripts/fix-datetime-kind.cs                         # default repo
//   dotnet run scripts/fix-datetime-kind.cs -- C:\…\main             # explicit repo

using System.Text.RegularExpressions;

var Repo = args.FirstOrDefault(A => Directory.Exists(A))
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var Src = Path.Combine(Repo, "src");
if (!Directory.Exists(Src))
{
    Console.Error.WriteLine($"src not found: {Src}");
    return 1;
}

var Files = Directory.EnumerateFiles(Src, "*.*", SearchOption.AllDirectories)
    .Where(P => P.EndsWith(".cs", StringComparison.Ordinal) || P.EndsWith(".razor", StringComparison.Ordinal))
    .Where(P => !P.Split(Path.DirectorySeparatorChar).Any(S => S is "bin" or "obj"))
    .ToList();

var DateRx = new Regex(@"new DateTime\(([^()]+)\)");
var Touched = 0;
var TotalSwaps = 0;
foreach (var F in Files)
{
    var Body = File.ReadAllText(F);
    var Local = 0;
    var Updated = DateRx.Replace(Body, M =>
    {
        var Args = M.Groups[1].Value;
        if (Args.Contains("DateTimeKind", StringComparison.Ordinal))
        {
            return M.Value;
        }
        Local++;
        return $"new DateTime({Args}, DateTimeKind.Local)";
    });
    if (Local > 0)
    {
        File.WriteAllText(F, Updated);
        Touched++;
        TotalSwaps += Local;
        Console.WriteLine($"  ✓ {Path.GetRelativePath(Repo, F)}  ({Local} swaps)");
    }
}

Console.WriteLine();
Console.WriteLine($"done — {Touched} files touched, {TotalSwaps} DateTime constructors updated");
return 0;
