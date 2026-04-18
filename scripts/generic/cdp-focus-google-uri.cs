#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-focus-google-uri.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpFocusGoogleUriPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);

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

var PageList = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages = CdpFocusGoogleUriPatterns.PageLine().Matches(PageList)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var Needle = Strings["Needle"];
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Needle, StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { await Console.Error.WriteLineAsync($"no tab matching: {Needle}"); return 3; }
await Console.Out.WriteLineAsync($"selecting page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {HitUrl}");
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

var Js = $"() => {{ const re = new RegExp('{Strings["AddButtonRegex"]}', 'i'); const buttons = Array.from(document.querySelectorAll('button, [role=button], a')); const target = buttons.find(b => re.test(b.textContent || '')); if (target) {{ target.scrollIntoView({{block: 'center'}}); target.click(); setTimeout(() => {{ const inputs = Array.from(document.querySelectorAll('input[type=text], input[type=url], input:not([type])')); const empty = inputs.find(i => !i.value); if (empty) {{ empty.focus(); empty.scrollIntoView({{block: 'center'}}); }} }}, 600); return 'clicked: ' + (target.textContent || target.getAttribute('aria-label') || target.tagName).trim().slice(0,80); }} return 'no add-uri button found; check page'; }}";
var EscapedJs = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedJs}\"");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpFocusGoogleUriPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
