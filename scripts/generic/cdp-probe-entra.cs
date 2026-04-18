#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-probe-entra.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpProbeEntraPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
foreach (var Required in new[] { "Needle", "ValueSubstring" })
{
    if (!Strings.ContainsKey(Required)) { await Console.Error.WriteLineAsync($"specific missing const string {Required}"); return 3; }
}

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
var Pages = CdpProbeEntraPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["Needle"], StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { await Console.Error.WriteLineAsync($"no tab matching: {Strings["Needle"]}"); return 4; }
await Console.Out.WriteLineAsync($"target page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {HitUrl[..Math.Min(HitUrl.Length, 120)]}");

var Js = $"() => {{ const valueSubstring = '{Strings["ValueSubstring"]}'; const inputs = Array.from(document.querySelectorAll('input')); const inputDetails = inputs.map(i => ({{ type: i.type, ariaLabel: i.getAttribute('aria-label') || '', placeholder: i.placeholder || '', value: (i.value || '').slice(0, 200), visible: i.offsetParent !== null }})).filter(d => d.visible); const buttons = Array.from(document.querySelectorAll('button, [role=button], a')).filter(b => b.offsetParent !== null).map(b => (b.textContent || b.getAttribute('aria-label') || '').trim()).filter(t => t.length > 0 && t.length < 100).slice(0, 50); const sectionLabels = Array.from(document.querySelectorAll('label, span, h1, h2, h3, h4, h5, [role=heading]')).map(e => (e.textContent || '').trim()).filter(t => t.length > 0 && t.length < 80 && (t.toLowerCase().includes('redirect') || t.toLowerCase().includes('uri') || t.toLowerCase().includes('platform') || t.toLowerCase().includes('web') || t.toLowerCase().includes('single-page'))); const matchValues = inputs.map(i => i.value || '').filter(v => v.includes(valueSubstring)); return JSON.stringify({{ url: location.href, title: document.title, inputCount: inputs.length, visibleInputCount: inputDetails.length, sampleInputs: inputDetails.slice(0, 30), buttons: buttons, sectionLabels: sectionLabels, matchValues: matchValues }}, null, 2); }}";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpProbeEntraPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
