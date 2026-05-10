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
var TenantKey = Get("TenantKey") ?? "Azure:TenantId";
var ClientIdKey = Get("ClientIdKey") ?? "Azure:ClientId";
var ClientSecretKey = Get("ClientSecretKey") ?? "Azure:ClientSecret";
var DefaultTenant = Get("DefaultTenant") ?? "common";
var DevtunnelExe = Get("DevtunnelExe") ?? "devtunnel";

using var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(SecretsJsonPath));
var Tenant = Doc.RootElement.TryGetProperty(TenantKey, out var T) ? T.GetString() ?? DefaultTenant : DefaultTenant;
var ClientId = Doc.RootElement.TryGetProperty(ClientIdKey, out var Ci) ? Ci.GetString() ?? string.Empty : string.Empty;
var Secret = Doc.RootElement.TryGetProperty(ClientSecretKey, out var Cs) ? Cs.GetString() ?? string.Empty : string.Empty;
if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(Secret))
{
    await Console.Error.WriteLineAsync("missing " + ClientIdKey + " or " + ClientSecretKey);
    return 2;
}

var Psi = new ProcessStartInfo(DevtunnelExe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
Psi.ArgumentList.Add("user");
Psi.ArgumentList.Add("login");
Psi.ArgumentList.Add("--sp-tenant-id");
Psi.ArgumentList.Add(Tenant);
Psi.ArgumentList.Add("--sp-client-id");
Psi.ArgumentList.Add(ClientId);
Psi.ArgumentList.Add("--sp-secret");
Psi.ArgumentList.Add(Secret);
using var Pp = Process.Start(Psi)!;
var Out = await Pp.StandardOutput.ReadToEndAsync();
var Err = await Pp.StandardError.ReadToEndAsync();
await Pp.WaitForExitAsync();
await Console.Out.WriteAsync(Out.Replace(Secret, "***", StringComparison.Ordinal));
if (!string.IsNullOrEmpty(Err)) await Console.Error.WriteAsync(Err.Replace(Secret, "***", StringComparison.Ordinal));
return Pp.ExitCode;
