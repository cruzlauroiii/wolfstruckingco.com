#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// regen-statics.cs - Specific. Owns the --in-place flag + repo root args
// for the canonical SharedUI prerender (item #24: no params on the
// PowerShell command line). Delegates to the GENERIC generate-statics.cs.
using System.Diagnostics;
using Scripts;

// Force fresh compile — file-based programs cache by content hash but
// occasionally serve stale dlls when the source was just edited.
var FbpCache = Path.Combine(Path.GetTempPath(), "wolfs-fbp-cache");
try { if (Directory.Exists(FbpCache)) { Directory.Delete(FbpCache, recursive: true); } } catch (IOException) { }
try { Directory.CreateDirectory(FbpCache); } catch (IOException) { }

var Psi = new ProcessStartInfo("dotnet", $"run scripts/generic/generate-statics.cs -- --in-place \"{Paths.Repo}\"")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = Paths.Repo,
};
Psi.EnvironmentVariables["DOTNET_CLI_HOME"] = FbpCache;
using var Proc = Process.Start(Psi)!;
var ReadOut = Proc.StandardOutput.ReadToEndAsync();
var ReadErr = Proc.StandardError.ReadToEndAsync();
await Proc.WaitForExitAsync().ConfigureAwait(false);
var OutText = await ReadOut.ConfigureAwait(false);
var ErrText = await ReadErr.ConfigureAwait(false);
if (Proc.ExitCode != 0)
{
    await Console.Error.WriteAsync(ErrText).ConfigureAwait(false);
    await Console.Error.WriteAsync(OutText).ConfigureAwait(false);
}
return Proc.ExitCode;
