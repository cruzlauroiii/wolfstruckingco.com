#:property TargetFramework=net11.0
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/set-worker-secrets.cs <specific.cs>"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var ConstRe = SecretsPatterns.ConstString();
var Map = new Dictionary<string, string>(StringComparer.Ordinal);
var SpecText = await File.ReadAllTextAsync(SpecPath);
foreach (var Pair in ConstRe.Matches(SpecText).Select(M => (M.Groups[1].Value, M.Groups[2].Value))) { Map[Pair.Item1] = Pair.Item2; }

string Need(string K) => Map.TryGetValue(K, out var V) ? V : throw new InvalidOperationException($"specific missing const string {K}");
var Provider = Need("Provider");
var ClientIdKey = Need("ClientIdKey");
var ClientSecretKey = Need("ClientSecretKey");
var SecretsJsonPath = Need("SecretsJsonPath");
var IdJsonKey = Need("IdJsonKey");
var SecretJsonKey = Need("SecretJsonKey");

if (!File.Exists(SecretsJsonPath)) { await Console.Error.WriteLineAsync($"secrets.json not found: {SecretsJsonPath}"); return 3; }
using var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(SecretsJsonPath));
string Get(string K) => Doc.RootElement.TryGetProperty(K, out var P) ? P.GetString() ?? string.Empty : string.Empty;
var ClientId = Get(IdJsonKey);
var ClientSecret = Get(SecretJsonKey);
if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
{
    await Console.Error.WriteLineAsync($"missing {IdJsonKey} or {SecretJsonKey} in {SecretsJsonPath}");
    return 4;
}

var CfEmail = Get("Cloudflare:Email");
var CfKey = Get("Cloudflare:GlobalApiKey");
if (string.IsNullOrEmpty(CfEmail) || string.IsNullOrEmpty(CfKey)) { await Console.Error.WriteLineAsync("missing Cloudflare:Email / Cloudflare:GlobalApiKey"); return 5; }

const string WorkerDir = @"C:\repo\public\wolfstruckingco.com\main\worker";
int PutSecret(string Name, string Value)
{
    var Psi = new ProcessStartInfo("cmd.exe", $"/c npx wrangler@latest secret put {Name}")
    {
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = WorkerDir,
    };
    Psi.EnvironmentVariables["CLOUDFLARE_EMAIL"] = CfEmail;
    Psi.EnvironmentVariables["CLOUDFLARE_API_KEY"] = CfKey;
    using var P = Process.Start(Psi)!;
    P.StandardInput.WriteLine(Value);
    P.StandardInput.Close();
    Console.Write(P.StandardOutput.ReadToEnd());
    Console.Error.Write(P.StandardError.ReadToEnd());
    P.WaitForExit();
    return P.ExitCode;
}
Console.WriteLine($"--- {Provider}: setting {ClientIdKey} ---");
var Code1 = PutSecret(ClientIdKey, ClientId);
Console.WriteLine($"--- {Provider}: setting {ClientSecretKey} ---");
var Code2 = PutSecret(ClientSecretKey, ClientSecret);
return Code1 != 0 ? Code1 : Code2;

namespace Scripts
{
    internal static partial class SecretsPatterns
    {
        [GeneratedRegex("""const\s+string\s+(\w+)\s*=\s*@?"((?:[^"\\]|\\.)*)"\s*;""")]
        internal static partial Regex ConstString();
    }
}
