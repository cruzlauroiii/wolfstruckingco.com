#:property TargetFramework=net11.0
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property EnableNETAnalyzers=false
#:property NoWarn=SA1503;SA1649;SA1633;SA1200;SA1201;SA1400;SA1502;SA1128;SA1519;SA1513;SA1516;SA1515;SA1413;IDE1006;RCS1001;RCS1003
#pragma warning disable SA1503, SA1649, SA1633, SA1200, SA1201, SA1400, SA1502, SA1128, SA1519, SA1513, SA1516, SA1515, SA1413, IDE1006, RCS1001, RCS1003
// Wolfs — SCSS → CSS build.
//
//   dotnet run scripts/build-scss.cs
//   dotnet run scripts/build-scss.cs -- --watch
//
// Compiles main/scss/wolfs.scss to main/wwwroot/wolfs.css via dart-sass.
// Install the compiler once with `npm install -g sass`. No other dependencies.

using System.Diagnostics;

var Watch = args.Contains("--watch") || args.Contains("-w");

// `dotnet run <file.cs>` stages the file into a temp runfile dir, so we can't infer
// the project root from BaseDirectory. Walk up looking for a sibling `scss/` folder.
var ProjectRoot = FindProjectRoot(Directory.GetCurrentDirectory())
               ?? FindProjectRoot(AppContext.BaseDirectory)
               ?? Directory.GetCurrentDirectory();
var Entry = Path.Combine(ProjectRoot, "scss", "wolfs.scss");
var Output = Path.Combine(ProjectRoot, "wwwroot", "wolfs.css");

if (!File.Exists(Entry))
{
    Console.Error.WriteLine($"SCSS entry not found at {Entry}. Run from the main/ folder.");
    return 1;
}
Directory.CreateDirectory(Path.GetDirectoryName(Output)!);

var SassArgs = new List<string>
{
    Entry,
    Output,
    "--style=compressed",
    "--no-source-map",
};
if (Watch) { SassArgs.Add("--watch"); }

Console.WriteLine($"sass {string.Join(' ', SassArgs)}");

var Psi = new ProcessStartInfo
{
    FileName = OperatingSystem.IsWindows() ? "sass.cmd" : "sass",
    UseShellExecute = false,
    RedirectStandardOutput = false,
    RedirectStandardError = false,
};
foreach (var A in SassArgs) { Psi.ArgumentList.Add(A); }

try
{
    using var Proc = Process.Start(Psi);
    if (Proc is null)
    {
        Console.Error.WriteLine("Failed to launch sass.");
        return 1;
    }
    Proc.WaitForExit();
    return Proc.ExitCode;
}
catch (System.ComponentModel.Win32Exception)
{
    Console.Error.WriteLine("sass not found on PATH. Install with: npm install -g sass");
    return 1;
}

static string? FindProjectRoot(string Start)
{
    var Dir = new DirectoryInfo(Start);
    while (Dir is not null)
    {
        if (Directory.Exists(Path.Combine(Dir.FullName, "scss")) &&
            Directory.Exists(Path.Combine(Dir.FullName, "wwwroot")))
        {
            return Dir.FullName;
        }
        Dir = Dir.Parent;
    }
    return null;
}
