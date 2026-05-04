#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string? Get(string Name)
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

var PyScript = Get("PyScript")!;
var ScenesPath = Get("ScenesPath")!;
var ModelOnnx = Get("ModelOnnx")!;
var VoicesBin = Get("VoicesBin")!;
var OutDir = Get("OutDir")!;
var Voice = Get("Voice") ?? "af_bella";

if (!File.Exists(PyScript) || !File.Exists(ScenesPath) || !File.Exists(ModelOnnx) || !File.Exists(VoicesBin))
{
    Console.WriteLine("PRECONDITION FAIL: missing input file");
    Console.WriteLine($"  PyScript: {PyScript} {(File.Exists(PyScript) ? "ok" : "MISSING")}");
    Console.WriteLine($"  ScenesPath: {ScenesPath} {(File.Exists(ScenesPath) ? "ok" : "MISSING")}");
    Console.WriteLine($"  ModelOnnx: {ModelOnnx} {(File.Exists(ModelOnnx) ? "ok" : "MISSING (download from https://github.com/thewh1teagle/kokoro-onnx/releases)")}");
    Console.WriteLine($"  VoicesBin: {VoicesBin} {(File.Exists(VoicesBin) ? "ok" : "MISSING (download from https://github.com/thewh1teagle/kokoro-onnx/releases)")}");
    return 4;
}

Directory.CreateDirectory(OutDir);

var Psi = new ProcessStartInfo("python") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
Psi.ArgumentList.Add(PyScript);
Psi.ArgumentList.Add(ScenesPath);
Psi.ArgumentList.Add(ModelOnnx);
Psi.ArgumentList.Add(VoicesBin);
Psi.ArgumentList.Add(OutDir);
Psi.ArgumentList.Add(Voice);

using var P = Process.Start(Psi)!;
var OutTask = P.StandardOutput.ReadToEndAsync();
var ErrTask = P.StandardError.ReadToEndAsync();
await Task.WhenAll(OutTask, ErrTask, P.WaitForExitAsync());
Console.Write(OutTask.Result);
if (P.ExitCode != 0)
{
    Console.Error.Write(ErrTask.Result);
}
return P.ExitCode;
