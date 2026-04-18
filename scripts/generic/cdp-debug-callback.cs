#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-debug-callback.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var NeedleMatch = CdpDebugCallbackPatterns.Needle().Match(Body);
if (!NeedleMatch.Success) { await Console.Error.WriteLineAsync("config missing const string Needle"); return 3; }
var Needle = NeedleMatch.Groups["needle"].Value;

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

var List = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages = CdpDebugCallbackPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Needle, StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { await Console.Error.WriteLineAsync($"no tab matching: {Needle}"); return 4; }
await Console.Out.WriteLineAsync($"selecting page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {HitUrl}");
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

var Js = "() => { const url = location.href; const html = document.documentElement.outerHTML.slice(0, 3000); return JSON.stringify({url: url, html: html}, null, 2); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\"");
await Console.Out.WriteLineAsync(Result);

await Console.Out.WriteLineAsync();
await Console.Out.WriteLineAsync("# Console messages on this page:");
var Console2 = await RunCdp(Paths.Cdp, Paths.Repo, "list_console_messages");
await Console.Out.WriteLineAsync(Console2);
return 0;

namespace Scripts
{
    internal static partial class CdpDebugCallbackPatterns
    {
        [GeneratedRegex("""const\s+string\s+Needle\s*=\s*"(?<needle>[^"]+)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex Needle();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
