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

var Voice = Get("Voice")!;
var Text = Get("Text")!;
var Out = Get("Out")!;
Directory.CreateDirectory(Path.GetDirectoryName(Out)!);

async Task<(int, string)> Try(string V)
{
    var Psi = new ProcessStartInfo("python") { RedirectStandardOutput = true, RedirectStandardError = true };
    Psi.ArgumentList.Add("-m");
    Psi.ArgumentList.Add("edge_tts");
    Psi.ArgumentList.Add("--voice"); Psi.ArgumentList.Add(V);
    Psi.ArgumentList.Add("--text"); Psi.ArgumentList.Add(Text);
    Psi.ArgumentList.Add("--write-media"); Psi.ArgumentList.Add(Out);
    using var P = Process.Start(Psi)!;
    var O = await P.StandardOutput.ReadToEndAsync();
    var E = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    return (P.ExitCode, E.Length > 0 ? E : O);
}

var (Ec, Ms) = await Try(Voice);
if (Ec == 0 && File.Exists(Out) && new FileInfo(Out).Length > 0) { await Console.Error.WriteLineAsync("edge-tts-probe ok voice=" + Voice + " bytes=" + new FileInfo(Out).Length); return 0; }
await Console.Error.WriteLineAsync("edge-tts-probe FAIL voice=" + Voice + " exit=" + Ec);
await Console.Error.WriteLineAsync("err: " + (Ms.Length > 800 ? Ms[(Ms.Length - 800)..] : Ms));
return Ec;
