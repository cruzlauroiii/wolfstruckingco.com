#:property TargetFramework=net11.0
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property EnableNETAnalyzers=false

// build-sharedui-scss.cs - compile src/SharedUI/scss/app.scss to
// src/SharedUI/wwwroot/css/app.css via the dart-sass CLI.
// generate-statics.cs reads that .css file and inlines it into every
// standalone HTML page.
//
//   dotnet run scripts/build-sharedui-scss.cs

using System.Diagnostics;

var Repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
if (!Directory.Exists(Path.Combine(Repo, "src", "SharedUI")))
{
    Repo = @"C:\repo\public\wolfstruckingco.com\main";
}

var In = Path.Combine(Repo, "src", "SharedUI", "scss", "app.scss");
var Out = Path.Combine(Repo, "src", "SharedUI", "wwwroot", "css", "app.css");
if (!File.Exists(In)) { Console.Error.WriteLine($"missing scss source: {In}"); return 1; }
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
    P.WaitForExit();
    var Err = P.StandardError.ReadToEnd();
    if (P.ExitCode != 0) { Console.Error.WriteLine(Err); return P.ExitCode; }
    Console.WriteLine($"  compiled {Path.GetFileName(In)} → {Path.GetRelativePath(Repo, Out)} ({new FileInfo(Out).Length:N0} bytes)");
}
catch (System.ComponentModel.Win32Exception)
{
    Console.Error.WriteLine("sass not on PATH. Install: npm install -g sass");
    return 1;
}
return 0;
