#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// build-razor-scss.cs — compile every Component.razor.scss → Component.razor.css
// in the SharedUI tree using dart-sass, so SharedUI components can author their
// scoped styles in SCSS. Blazor's CSS-isolation pipeline auto-bundles the
// resulting .razor.css files at dotnet build time.
//
//   dotnet run scripts/build-razor-scss.cs                       # one-shot
//   dotnet run scripts/build-razor-scss.cs -- --watch             # watch mode
//   dotnet run scripts/build-razor-scss.cs -- --root <repo-root>  # alternate root
//
// Requires `npm install -g sass` once.

using System.Diagnostics;

var Watch = args.Contains("--watch");
var Root = args.Length > 0 && Array.IndexOf(args, "--root") is var I && I >= 0 && I + 1 < args.Length
    ? args[I + 1]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var SharedUi = Path.Combine(Root, "src", "SharedUI");
if (!Directory.Exists(SharedUi))
{
    Console.Error.WriteLine($"SharedUI not found: {SharedUi}");
    return 1;
}

string Sass = OperatingSystem.IsWindows() ? "sass.cmd" : "sass";

int Compile(string SrcFile)
{
    var Dst = SrcFile[..^5] + ".css";  // foo.razor.scss → foo.razor.css
    var P = new ProcessStartInfo(Sass, $"--no-source-map --style=expanded \"{SrcFile}\":\"{Dst}\"")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    using var Proc = Process.Start(P)!;
    Proc.WaitForExit();
    var Err = Proc.StandardError.ReadToEnd();
    if (Proc.ExitCode != 0)
    {
        Console.Error.WriteLine($"  ✗ {Path.GetRelativePath(Root, SrcFile)}");
        Console.Error.WriteLine(Err);
    }
    else
    {
        Console.WriteLine($"  ✓ {Path.GetRelativePath(Root, SrcFile)} → {Path.GetFileName(Dst)}");
    }
    return Proc.ExitCode;
}

int CompileAll()
{
    var Files = Directory.EnumerateFiles(SharedUi, "*.razor.scss", SearchOption.AllDirectories).ToList();
    if (Files.Count == 0)
    {
        Console.WriteLine($"(no .razor.scss files found under {SharedUi})");
        return 0;
    }
    var Failed = 0;
    foreach (var F in Files)
    {
        if (Compile(F) != 0)
        {
            Failed++;
        }
    }
    Console.WriteLine($"compiled {Files.Count - Failed}/{Files.Count} component SCSS file(s)");
    return Failed;
}

var First = CompileAll();
if (!Watch)
{
    return First;
}

Console.WriteLine($"watching {SharedUi} for *.razor.scss changes (Ctrl+C to stop)…");
using var Fsw = new FileSystemWatcher(SharedUi, "*.razor.scss") { IncludeSubdirectories = true };
Fsw.Changed += (_, E) => { try { Compile(E.FullPath); } catch { } };
Fsw.Created += (_, E) => { try { Compile(E.FullPath); } catch { } };
Fsw.Renamed += (_, E) => { try { Compile(E.FullPath); } catch { } };
Fsw.EnableRaisingEvents = true;
await Task.Delay(Timeout.Infinite);
return 0;
