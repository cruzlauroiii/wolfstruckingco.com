#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;

async Task<bool> Probe(string Label, string FileName, string Args)
{
    var Psi = new ProcessStartInfo(FileName, Args) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    try
    {
        using var P = Process.Start(Psi);
        if (P is null) { await Console.Out.WriteLineAsync($"[{Label}] ✗ no process"); return false; }
        var O = P.StandardOutput.ReadToEndAsync();
        var E = P.StandardError.ReadToEndAsync();
        await P.WaitForExitAsync();
        var Ok = P.ExitCode == 0;
        var Out = (await O).Trim();
        var Err = (await E).Trim();
        var First = (Out.Length > 0 ? Out : Err).Split('\n')[0];
        await Console.Out.WriteLineAsync($"[{Label}] {(Ok ? "✓" : "✗")} exit {P.ExitCode}  {First}");
        return Ok;
    }
    catch (Exception X) { await Console.Out.WriteLineAsync($"[{Label}] ✗ {X.Message}"); return false; }
}

await Console.Out.WriteLineAsync("=== TTS engine availability probe ===");
await Probe("python", "python", "--version");
await Probe("ffmpeg", "ffmpeg", "-version");
await Probe("ffprobe", "ffprobe", "-version");
await Probe("bark", "python", "-c \"import bark; print(bark.__version__ if hasattr(bark, '__version__') else 'installed')\"");
await Probe("TTS (Coqui)", "python", "-c \"import TTS; print(TTS.__version__)\"");
await Probe("tortoise", "python", "-c \"import tortoise; print('installed')\"");
await Probe("openvoice", "python", "-c \"import openvoice_cli; print('installed')\"");
await Probe("GPT-SoVITS", "powershell", "-NoProfile -Command \"Test-Path 'C:\\tools\\GPT-SoVITS\\inference_cli.py'\"");
await Console.Out.WriteLineAsync("");
await Console.Out.WriteLineAsync("For each ✗, install per SCRIPTS.md before running build-walkthrough-local.cs.");
return 0;
