#:property TargetFramework=net11.0
using System.Diagnostics;

var Repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
if (!Directory.Exists(Path.Combine(Repo, "src", "SharedUI"))) { Repo = @"C:\repo\public\wolfstruckingco.com\main"; }

var In = Path.Combine(Repo, "src", "SharedUI", "scss", "app.scss");
var Out = Path.Combine(Repo, "src", "SharedUI", "wwwroot", "css", "app.css");
if (!File.Exists(In)) { await Console.Error.WriteLineAsync($"missing scss source: {In}"); return 1; }
Directory.CreateDirectory(Path.GetDirectoryName(Out)!);

var Sass = OperatingSystem.IsWindows() ? "sass.cmd" : "sass";
var Psi = new ProcessStartInfo(Sass)
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
foreach (var A in new[] { In, Out, "--style=compressed", "--no-source-map" }) { Psi.ArgumentList.Add(A); }

try
{
    using var P = Process.Start(Psi)!;
    await P.WaitForExitAsync();
    if (P.ExitCode != 0) { await Console.Error.WriteLineAsync((await P.StandardError.ReadToEndAsync()).Trim()); return P.ExitCode; }
}
catch (System.ComponentModel.Win32Exception)
{
    await Console.Error.WriteLineAsync("sass not on PATH (npm install -g sass)");
    return 1;
}
return 0;
