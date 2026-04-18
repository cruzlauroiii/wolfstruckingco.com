#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-probe-tab.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpProbeTabPatterns.ConstString().Matches(Body)
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
var Pages = CdpProbeTabPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["Needle"], StringComparison.OrdinalIgnoreCase));
if (HitUrl is null)
{
    await Console.Error.WriteLineAsync($"no tab matching: {Strings["Needle"]}");
    foreach (var (Idx, Url) in Pages) { await Console.Out.WriteLineAsync($"  {Idx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {Url[..Math.Min(Url.Length, 120)]}"); }
    return 3;
}
await Console.Out.WriteLineAsync($"page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {HitUrl[..Math.Min(HitUrl.Length, 120)]}");

var Js = "() => { const url = location.href; const title = document.title; const h1 = (document.querySelector('h1, h2') || {innerText: ''}).innerText || ''; const errs = Array.from(document.querySelectorAll('[class*=error], [class*=Error], [role=alert]')).map(e => (e.innerText || '').trim()).filter(t => t.length > 0 && t.length < 300); const buttons = Array.from(document.querySelectorAll('button, a[role=button], input[type=submit]')).slice(0, 20).map(b => (b.innerText || b.value || '').trim()).filter(t => t.length > 0 && t.length < 80); const visibleText = (document.body.innerText || '').slice(0, 1500); return JSON.stringify({url: url, title: title, h1: h1, errors: errs, buttons: buttons, bodySnippet: visibleText}, null, 2); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpProbeTabPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
