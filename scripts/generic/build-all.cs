#:property TargetFramework=net11.0
using System.Diagnostics;

var Repo = args.FirstOrDefault(A => !A.StartsWith("--", StringComparison.Ordinal))
    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var InPlace = args.Contains("--in-place");
var SkipPublish = args.Contains("--skip-publish");
var Scripts = Path.Combine(Repo, "scripts");

var Steps = new List<(string Name, string Script, string ExtraArgs)>
{
    ("component-scss", "build-razor-scss.cs", $"--root \"{Repo}\""),
    ("global-scss",    "build-scss.cs",       string.Empty),
    ("publish-wasm",   "publish-pages.cs",    $"--repo \"{Repo}\""),
    ("static-html",    "generate-statics.cs", $"\"{Repo}\"" + (InPlace ? " --in-place" : string.Empty)),
};

if (SkipPublish) { Steps.RemoveAll(S => S.Name == "publish-wasm"); }

var Failed = new List<string>();
foreach (var (Name, Script, ExtraArgs) in Steps)
{
    var ScriptPath = Path.Combine(Scripts, Script);
    if (!File.Exists(ScriptPath)) { await Console.Error.WriteLineAsync($"skip {Name}"); continue; }
    var Psi = new ProcessStartInfo("dotnet", $"run \"{ScriptPath}\" -- {ExtraArgs}".TrimEnd())
    {
        UseShellExecute = false,
        WorkingDirectory = Repo,
    };
    using var Proc = Process.Start(Psi)!;
    await Proc.WaitForExitAsync();
    if (Proc.ExitCode != 0) { Failed.Add(Name); }
}

if (Failed.Count == 0) { return 0; }
await Console.Error.WriteLineAsync($"failed: {string.Join(", ", Failed)}");
return 1;
