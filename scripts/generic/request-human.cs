#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var Specs = await File.ReadAllLinesAsync(SpecPath);
string? Read(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        if (After.StartsWith("@", StringComparison.Ordinal)) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}

var Persona = Read("Persona") ?? "unknown";
var Provider = Read("Provider") ?? "unknown";
var UrlNeeded = Read("UrlNeeded") ?? "";
var Action = Read("Action") ?? "sign in";
var HandoffJsonPath = Read("HandoffJsonPath") ?? Path.Combine(Path.GetTempPath(), "wolfs-human-needed.json");
var AckPath = Read("AckPath") ?? Path.Combine(Path.GetTempPath(), "wolfs-alarm-ack.txt");
var AlarmGeneric = Read("AlarmGeneric") ?? @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\alarm-human.cs";
var AlarmConfig = Read("AlarmConfig") ?? @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\alarm-human-scratch-config.cs";
var Repo = Read("Repo") ?? @"C:\repo\public\wolfstruckingco.com\main";

var Headline = $"Wolfs needs {Persona} to {Action} via {Provider}";
var Body = string.IsNullOrEmpty(UrlNeeded) ? Action : $"{Action} at {UrlNeeded}";
var Json = $"{{\"persona\":\"{Persona}\",\"provider\":\"{Provider}\",\"url\":\"{UrlNeeded.Replace(\"\\\"\", \"\\\\\\\"\", StringComparison.Ordinal)}\",\"action\":\"{Action.Replace(\"\\\"\", \"\\\\\\\"\", StringComparison.Ordinal)}\",\"headline\":\"{Headline}\",\"body\":\"{Body}\",\"ackPath\":\"{AckPath.Replace(\"\\\\\", \"\\\\\\\\\\\\", StringComparison.Ordinal)}\",\"requestedAt\":\"{DateTimeOffset.UtcNow:O}\"}}";
await File.WriteAllTextAsync(HandoffJsonPath, Json);

var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Repo };
Psi.ArgumentList.Add("run");
Psi.ArgumentList.Add(AlarmGeneric);
Psi.ArgumentList.Add(AlarmConfig);
using var P = Process.Start(Psi)!;
await P.WaitForExitAsync();
if (P.ExitCode != 0) return 42;
return 0;
