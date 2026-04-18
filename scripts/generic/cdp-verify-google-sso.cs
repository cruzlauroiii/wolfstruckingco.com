#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-verify-google-sso.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Url0Match = CdpVerifyGoogleSsoPatterns.Url0().Match(Body);
if (!Url0Match.Success) { await Console.Error.WriteLineAsync("config missing const string Url0"); return 3; }
var ProbeUrl = Url0Match.Groups["url"].Value;

static async Task<string> RunCdp(string Cdp, string Repo, string Command)
{
    var Psi = new ProcessStartInfo("dotnet", $"run \"{Cdp}\" -- {Command}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Repo,
    };
    using var Proc = Process.Start(Psi)!;
    var Out = await Proc.StandardOutput.ReadToEndAsync();
    await Proc.WaitForExitAsync();
    return Out;
}

_ = await RunCdp(Paths.Cdp, Paths.Repo, $"new_page \"{ProbeUrl}\"");
await Task.Delay(4000);

var ListAfter = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
await Console.Out.WriteLineAsync("# Pages after navigation");
await Console.Out.WriteLineAsync(ListAfter);

var Js = "() => { const u = location.href; const t = document.title; const bodyText = (document.body && document.body.innerText || '').slice(0, 600); return JSON.stringify({url: u, title: t, snippet: bodyText}, null, 2); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\"");
await Console.Out.WriteLineAsync("# Current page state");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpVerifyGoogleSsoPatterns
    {
        [GeneratedRegex("""const\s+string\s+Url0\s*=\s*"(?<url>[^"]+)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex Url0();
    }
}
