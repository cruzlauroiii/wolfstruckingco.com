#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

if (args.Length < 1) return 1;
var specPath = args[0];
if (!File.Exists(specPath)) return 2;

var concatConfig = Path.Combine(Environment.CurrentDirectory, "scripts", "specific", "concat-scene-videos-config.cs");
if (!File.Exists(concatConfig))
{
    Console.Error.WriteLine("Missing concat config: " + concatConfig);
    return 3;
}

var start = new ProcessStartInfo("dotnet")
{
    WorkingDirectory = Environment.CurrentDirectory,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};
start.ArgumentList.Add("run");
start.ArgumentList.Add("scripts\\generic\\concat-scene-videos.cs");
start.ArgumentList.Add("scripts\\specific\\concat-scene-videos-config.cs");

using var child = Process.Start(start)!;
var stdout = child.StandardOutput.ReadToEndAsync();
var stderr = child.StandardError.ReadToEndAsync();
var wait = child.WaitForExitAsync();
if (await Task.WhenAny(wait, Task.Delay(TimeSpan.FromMinutes(30))) != wait)
{
    child.Kill(true);
    Console.Error.WriteLine("rebuild-walkthrough timed out while concatenating scene videos");
    return 124;
}

Console.Write(await stdout);
Console.Error.Write(await stderr);
return child.ExitCode;
