#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-probe-okta.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpProbeOktaPatterns.ConstString().Matches(Body)
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

var List = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages = CdpProbeOktaPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["Needle"], StringComparison.OrdinalIgnoreCase));
if (HitUrl is null)
{
    await Console.Error.WriteLineAsync($"no admin tab matching: {Strings["Needle"]}");
    await Console.Out.WriteLineAsync("available pages:");
    foreach (var (Idx, Url) in Pages) { await Console.Out.WriteLineAsync($"  {Idx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {Url[..Math.Min(Url.Length, 120)]}"); }
    return 3;
}
await Console.Out.WriteLineAsync($"found page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {HitUrl[..Math.Min(HitUrl.Length, 120)]}");
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Task.Delay(1000);

var Js = "() => { const url = location.href; const title = document.title; const errs = Array.from(document.querySelectorAll('[class*=error], [class*=Error], [role=alert], .infobox-error, .o-form-error-container')).map(e => (e.innerText || '').trim()).filter(t => t.length > 0); const banners = Array.from(document.querySelectorAll('[class*=banner], [class*=Banner], [class*=warning], [class*=Warning]')).map(e => (e.innerText || '').trim()).filter(t => t.length > 0 && t.length < 500); const visibleText = (document.body.innerText || '').slice(0, 3000); return JSON.stringify({url: url, title: title, errors: errs, banners: banners, bodySnippet: visibleText}, null, 2); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpProbeOktaPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
