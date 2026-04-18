#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// find-js-files.cs - Specific. Owns the repo root + the directories that
// must be JS-free (item #38, item #7-current). Walks each, prints every
// .js file with its size and whether it's tracked by git. No CLI args.
using System.Diagnostics;
using Scripts;

string[] Roots = ["src", "docs", "wwwroot", "scripts", "worker"];

static async Task<IReadOnlySet<string>> GetTracked(string Repo)
{
    var Psi = new ProcessStartInfo("git", "ls-files") { RedirectStandardOutput = true, UseShellExecute = false, WorkingDirectory = Repo };
    using var P = Process.Start(Psi)!;
    var Set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    string? Line;
    while ((Line = await P.StandardOutput.ReadLineAsync()) != null)
    {
        Set.Add(Path.Combine(Repo, Line.Replace('/', Path.DirectorySeparatorChar)));
    }
    await P.WaitForExitAsync();
    return Set;
}

var Tracked = await GetTracked(Paths.Repo);
var Total = 0;
var Tracked2 = 0;
foreach (var R in Roots)
{
    var Full = Path.Combine(Paths.Repo, R);
    if (!Directory.Exists(Full)) { continue; }
    foreach (var F in Directory.GetFiles(Full, "*.js", SearchOption.AllDirectories))
    {
        if (F.Contains(@"\app\_framework\", StringComparison.OrdinalIgnoreCase)) { continue; }
        if (F.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase)) { continue; }
        if (F.Contains(@"\obj\", StringComparison.OrdinalIgnoreCase)) { continue; }
        var Rel = F[(Paths.Repo.Length + 1)..];
        var Size = new FileInfo(F).Length;
        var IsTracked = Tracked.Contains(F);
        await Console.Out.WriteLineAsync($"{Size.ToString(System.Globalization.CultureInfo.InvariantCulture),8}  {(IsTracked ? "T" : " ")}  {Rel}");
        Total++;
        if (IsTracked) { Tracked2++; }
    }
}
await Console.Out.WriteLineAsync($"--- {Total.ToString(System.Globalization.CultureInfo.InvariantCulture)} .js files total, {Tracked2.ToString(System.Globalization.CultureInfo.InvariantCulture)} tracked ---");
return 0;
