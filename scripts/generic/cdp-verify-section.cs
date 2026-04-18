#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-verify-section.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpVerifySectionPatterns.ConstString().Matches(Body)
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
var Pages = CdpVerifySectionPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["Needle"], StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { await Console.Error.WriteLineAsync("no edit-page tab"); return 3; }
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Task.Delay(1500);

var Js = $"() => {{ const target = '{Strings["TargetValue"]}'; const expected = '{Strings["ExpectedSection"]}'; const other = '{Strings["OtherSection"]}'; const inputs = Array.from(document.querySelectorAll('input')); const match = inputs.find(i => (i.value || '').trim() === target); if (!match) return JSON.stringify({{found: false}}); let parent = match; let sectionLabel = ''; let depth = 0; while (parent && depth < 30) {{ const text = (parent.textContent || '').trim(); if (text.includes(expected) && !text.includes(other + '\\nhttps')) {{ sectionLabel = expected; break; }} if (text.includes(other) && !text.includes(expected + '\\nhttps')) {{ sectionLabel = other; break; }} parent = parent.parentElement; depth++; }} const allHeaders = Array.from(document.querySelectorAll('h1, h2, h3, h4, [role=heading]')).map(h => (h.textContent || '').trim()).filter(t => t.length); return JSON.stringify({{found: true, value: target, focused: match === document.activeElement, exactByteLength: target.length, sectionDetected: sectionLabel || 'UNKNOWN', headersOnPage: allHeaders.slice(0, 20)}}, null, 2); }}";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\"");
await Console.Out.WriteLineAsync(Result);
await Console.Out.WriteLineAsync();
await Console.Out.WriteLineAsync($"Expected exact match: '{Strings["TargetValue"]}'");
await Console.Out.WriteLineAsync($"Expected section: '{Strings["ExpectedSection"]}'");
return 0;

namespace Scripts
{
    internal static partial class CdpVerifySectionPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
