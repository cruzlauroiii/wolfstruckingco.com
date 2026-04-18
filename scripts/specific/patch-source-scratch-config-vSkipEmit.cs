namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVSkipEmit
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\deploy-worker.cs";
    public const string Find_01 = "var EmitPsi = new ProcessStartInfo(\"dotnet\", \"run worker/worker.cs\") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Paths.Repo };\nusing (var EmitProc = Process.Start(EmitPsi)!)\n{\n    await Console.Out.WriteAsync(await EmitProc.StandardOutput.ReadToEndAsync());\n    await Console.Error.WriteAsync(await EmitProc.StandardError.ReadToEndAsync());\n    await EmitProc.WaitForExitAsync();\n    if (EmitProc.ExitCode != 0) { await Console.Error.WriteLineAsync(\"emit-worker failed\"); return EmitProc.ExitCode; }\n}";
    public const string Replace_01 = "if (!File.Exists(DeployWorkerPaths.TransientJs))\n{\n    var EmitPsi = new ProcessStartInfo(\"dotnet\", \"run worker/worker.cs\") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Paths.Repo };\n    using var EmitProc = Process.Start(EmitPsi)!;\n    await Console.Out.WriteAsync(await EmitProc.StandardOutput.ReadToEndAsync());\n    await Console.Error.WriteAsync(await EmitProc.StandardError.ReadToEndAsync());\n    await EmitProc.WaitForExitAsync();\n    if (EmitProc.ExitCode != 0) { await Console.Error.WriteLineAsync(\"emit-worker failed\"); return EmitProc.ExitCode; }\n}";
    public const string Find_02 = "try { if (File.Exists(DeployWorkerPaths.TransientJs)) { File.Delete(DeployWorkerPaths.TransientJs); } }";
    public const string Replace_02 = "try { /* keep worker.js as source of truth */ }";
}
