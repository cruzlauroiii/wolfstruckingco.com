#:property TargetFramework=net11.0
using System.Diagnostics;

var Watch = args.Contains("--watch");
var Root = args.Length > 0 && Array.IndexOf(args, "--root") is var I && I >= 0 && I + 1 < args.Length
    ? args[I + 1]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var SharedUi = Path.Combine(Root, "src", "SharedUI");
if (!Directory.Exists(SharedUi)) { await Console.Error.WriteLineAsync($"SharedUI not found: {SharedUi}"); return 1; }

var Sass = OperatingSystem.IsWindows() ? "sass.cmd" : "sass";

async Task<int> Compile(string SrcFile)
{
    var Dst = SrcFile[..^5] + ".css";
    var P = new ProcessStartInfo(Sass, $"--no-source-map --style=expanded \"{SrcFile}\":\"{Dst}\"")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    using var Proc = Process.Start(P)!;
    await Proc.WaitForExitAsync();
    if (Proc.ExitCode != 0) { await Console.Error.WriteLineAsync($"{Path.GetRelativePath(Root, SrcFile)}: {(await Proc.StandardError.ReadToEndAsync()).Trim()}"); }
    return Proc.ExitCode;
}

async Task<int> CompileAll()
{
    var Files = Directory.EnumerateFiles(SharedUi, "*.razor.scss", SearchOption.AllDirectories).ToList();
    if (Files.Count == 0) { return 0; }
    var Failed = 0;
    foreach (var F in Files) { if (await Compile(F) != 0) { Failed++; } }
    return Failed;
}

var First = await CompileAll();
if (!Watch) { return First; }

using var Fsw = new FileSystemWatcher(SharedUi, "*.razor.scss") { IncludeSubdirectories = true };
Fsw.Changed += async (_, E) => { try { await Compile(E.FullPath); } catch (IOException Ex) { await Console.Error.WriteLineAsync(Ex.Message); } };
Fsw.Created += async (_, E) => { try { await Compile(E.FullPath); } catch (IOException Ex) { await Console.Error.WriteLineAsync(Ex.Message); } };
Fsw.Renamed += async (_, E) => { try { await Compile(E.FullPath); } catch (IOException Ex) { await Console.Error.WriteLineAsync(Ex.Message); } };
Fsw.EnableRaisingEvents = true;
await Task.Delay(Timeout.Infinite);
return 0;
