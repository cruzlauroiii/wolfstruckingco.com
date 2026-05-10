#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.Json;

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

var SecretsJsonPath = Get("SecretsJsonPath")!;
var SecretKey = Get("SecretKey") ?? "GitHub:Pat";
var DevtunnelExe = Get("DevtunnelExe") ?? "devtunnel";

using var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(SecretsJsonPath));
var Pat = Doc.RootElement.TryGetProperty(SecretKey, out var P) ? P.GetString() ?? string.Empty : string.Empty;
if (string.IsNullOrEmpty(Pat))
{
    await Console.Error.WriteLineAsync("PAT not found at " + SecretKey);
    return 2;
}

var Psi = new ProcessStartInfo(DevtunnelExe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
Psi.ArgumentList.Add("user");
Psi.ArgumentList.Add("login");
Psi.ArgumentList.Add("-g");
Psi.ArgumentList.Add(Pat);
using var Pp = Process.Start(Psi)!;
var Out = await Pp.StandardOutput.ReadToEndAsync();
var Err = await Pp.StandardError.ReadToEndAsync();
await Pp.WaitForExitAsync();
await Console.Out.WriteAsync(Out.Replace(Pat, "***", StringComparison.Ordinal));
if (!string.IsNullOrEmpty(Err)) await Console.Error.WriteAsync(Err.Replace(Pat, "***", StringComparison.Ordinal));
return Pp.ExitCode;
