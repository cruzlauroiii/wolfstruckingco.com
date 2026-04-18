#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using Scripts;

string[] Roots = ["src", "docs", "wwwroot", "scripts"];

static bool IsKeep(string F) =>
    F.Contains(@"\worker\", StringComparison.OrdinalIgnoreCase)
    || F.Contains(@"\_framework\", StringComparison.OrdinalIgnoreCase)
    || F.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase)
    || F.Contains(@"\obj\", StringComparison.OrdinalIgnoreCase);

var Deleted = 0;
foreach (var R in Roots)
{
    var Full = Path.Combine(Paths.Repo, R);
    if (!Directory.Exists(Full)) { continue; }
    foreach (var F in Directory.GetFiles(Full, "*.js", SearchOption.AllDirectories))
    {
        if (IsKeep(F)) { continue; }
        try { File.Delete(F); Deleted++; }
        catch (IOException Ex) { await Console.Error.WriteLineAsync($"fail {F}: {Ex.Message}"); }
    }
}

var Psi = new ProcessStartInfo("git", "rm -rf --cached --ignore-unmatch docs/*.js wwwroot/*.js docs/Dashboard/*.js wwwroot/Dashboard/*.js docs/app/_content/SharedUI/js/*.js wwwroot/app/_content/SharedUI/js/*.js src/Client/wwwroot/voice-bridge.js wwwroot/app/voice-bridge.js src/SharedUI/wwwroot/js/*.js")
{ RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = Paths.Repo };
using var Proc = Process.Start(Psi)!;
await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
await Proc.WaitForExitAsync();

if (Deleted > 0) { await Console.Out.WriteLineAsync($"deleted {Deleted.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
return 0;
