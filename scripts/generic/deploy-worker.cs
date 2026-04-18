#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
#:include ../specific/deploy-worker-config.cs
using System.Diagnostics;
using System.Text.Json;
using Scripts;

if (!File.Exists(DeployWorkerPaths.TransientJs))
{
    var EmitPsi = new ProcessStartInfo("dotnet", "run worker/worker.cs") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Paths.Repo };
    using var EmitProc = Process.Start(EmitPsi)!;
    await Console.Out.WriteAsync(await EmitProc.StandardOutput.ReadToEndAsync());
    await Console.Error.WriteAsync(await EmitProc.StandardError.ReadToEndAsync());
    await EmitProc.WaitForExitAsync();
    if (EmitProc.ExitCode != 0) { await Console.Error.WriteLineAsync("emit-worker failed"); return EmitProc.ExitCode; }
}

if (!File.Exists(DeployWorkerPaths.SecretsPath)) { await Console.Error.WriteLineAsync($"secrets.json not found: {DeployWorkerPaths.SecretsPath}"); return 2; }
using var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(DeployWorkerPaths.SecretsPath));
var Email = Doc.RootElement.TryGetProperty("Cloudflare:Email", out var E) ? E.GetString() : null;
var GlobalKey = Doc.RootElement.TryGetProperty("Cloudflare:GlobalApiKey", out var G) ? G.GetString() : null;
var Token = Doc.RootElement.TryGetProperty("Cloudflare:ApiToken", out var T) ? T.GetString() : null;
if (string.IsNullOrEmpty(Token) && (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(GlobalKey)))
{
    await Console.Error.WriteLineAsync("missing Cloudflare creds in secrets.json");
    return 3;
}

var Psi = new ProcessStartInfo("cmd.exe", "/c npx wrangler@latest deploy --minify")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = DeployWorkerPaths.WorkerDir,
};
if (!string.IsNullOrEmpty(Token)) { Psi.EnvironmentVariables["CLOUDFLARE_API_TOKEN"] = Token; }
else
{
    Psi.EnvironmentVariables["CLOUDFLARE_EMAIL"] = Email;
    Psi.EnvironmentVariables["CLOUDFLARE_API_KEY"] = GlobalKey;
}
int Code;
using (var Proc = Process.Start(Psi)!)
{
    await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
    await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
    await Proc.WaitForExitAsync();
    Code = Proc.ExitCode;
}
return Code;
