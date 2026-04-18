#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// build-all.cs — single-entry orchestrator that runs the full SharedUI pipeline
// in order: component-scoped SCSS → global SCSS → WASM publish → static HTML
// generation. Each step is a separate `dotnet run scripts/<file>.cs` so each can
// be inspected, re-run, or skipped on its own; this script just chains them.
//
//   dotnet run scripts/build-all.cs                            # default repo
//   dotnet run scripts/build-all.cs -- --in-place              # also overwrite docs/<Route>/
//   dotnet run scripts/build-all.cs -- --skip-publish          # skip the WASM publish step
//   dotnet run scripts/build-all.cs -- C:\…\main               # explicit repo root

using System.Diagnostics;

var Repo = args.Where(A => !A.StartsWith("--")).FirstOrDefault()
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var InPlace = args.Contains("--in-place");
var SkipPublish = args.Contains("--skip-publish");
var Scripts = Path.Combine(Repo, "scripts");

var Steps = new List<(string Name, string Script, string ExtraArgs)>
{
    ("component-scss", "build-razor-scss.cs", $"--root \"{Repo}\""),
    ("global-scss",    "build-scss.cs",       ""),
    ("publish-wasm",   "publish-pages.cs",    $"--repo \"{Repo}\""),
    ("static-html",    "generate-statics.cs", $"\"{Repo}\"" + (InPlace ? " --in-place" : "")),
};

if (SkipPublish)
{
    Steps.RemoveAll(S => S.Name == "publish-wasm");
}

var Failed = new List<string>();
var Sw = Stopwatch.StartNew();
foreach (var (Name, Script, ExtraArgs) in Steps)
{
    var ScriptPath = Path.Combine(Scripts, Script);
    if (!File.Exists(ScriptPath))
    {
        Console.Error.WriteLine($"[skip] {Name} — {Script} not found");
        continue;
    }
    Console.WriteLine();
    Console.WriteLine($"━━━━━━━━━━━━━━━━ {Name} ━━━━━━━━━━━━━━━━");
    var StepStart = Stopwatch.StartNew();
    var Psi = new ProcessStartInfo("dotnet", $"run \"{ScriptPath}\" -- {ExtraArgs}".TrimEnd())
    {
        UseShellExecute = false,
        WorkingDirectory = Repo,
    };
    using var Proc = Process.Start(Psi)!;
    Proc.WaitForExit();
    StepStart.Stop();
    Console.WriteLine($"   ↳ {Name} {(Proc.ExitCode == 0 ? "✓" : "✗")} in {StepStart.Elapsed.TotalSeconds:F1}s");
    if (Proc.ExitCode != 0)
    {
        Failed.Add(Name);
    }
}

Sw.Stop();
Console.WriteLine();
Console.WriteLine($"━━ build-all done in {Sw.Elapsed.TotalSeconds:F1}s ━━");
if (Failed.Count == 0)
{
    Console.WriteLine("all steps green");
    return 0;
}
Console.Error.WriteLine($"failed: {string.Join(", ", Failed)}");
return 1;
