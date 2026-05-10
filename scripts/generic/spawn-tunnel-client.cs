#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != '\u0022') continue;
        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var Wd = Get("Wd") ?? Environment.GetEnvironmentVariable("WOLFS_REPO") ?? Environment.CurrentDirectory;
var ClientConfig = Get("ClientConfig")!;

var Psi = new ProcessStartInfo("dotnet")
{
    UseShellExecute = true,
    WorkingDirectory = Wd,
    CreateNoWindow = false,
};
Psi.ArgumentList.Add("run");
Psi.ArgumentList.Add(@"main\scripts\generic\tunnel-client.cs");
Psi.ArgumentList.Add(ClientConfig);
var Proc = Process.Start(Psi)!;
await Console.Error.WriteLineAsync("spawn-tunnel-client pid=" + Proc.Id + " config=" + ClientConfig);
await Task.Delay(8000);
return 0;
