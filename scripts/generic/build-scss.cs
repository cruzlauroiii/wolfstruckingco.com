#:property TargetFramework=net11.0
using System.Diagnostics;

var Watch = args.Contains("--watch") || args.Contains("-w");
var ProjectRoot = FindProjectRoot(Directory.GetCurrentDirectory())
               ?? FindProjectRoot(AppContext.BaseDirectory)
               ?? Directory.GetCurrentDirectory();
var Entry = Path.Combine(ProjectRoot, "scss", "wolfs.scss");
var Output = Path.Combine(ProjectRoot, "wwwroot", "wolfs.css");

if (!File.Exists(Entry)) { await Console.Error.WriteLineAsync($"SCSS entry not found at {Entry}"); return 1; }
Directory.CreateDirectory(Path.GetDirectoryName(Output)!);

var SassArgs = new List<string> { Entry, Output, "--style=compressed", "--no-source-map" };
if (Watch) { SassArgs.Add("--watch"); }

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
    if (Proc is null) { await Console.Error.WriteLineAsync("sass start failed"); return 1; }
    await Proc.WaitForExitAsync();
    return Proc.ExitCode;
}
catch (System.ComponentModel.Win32Exception)
{
    await Console.Error.WriteLineAsync("sass not on PATH (npm install -g sass)");
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
