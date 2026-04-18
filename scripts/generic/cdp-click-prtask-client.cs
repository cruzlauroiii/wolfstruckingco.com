#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-click-prtask-client.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpClickPrTaskPatterns.ConstString().Matches(Body)
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
var Pages = CdpClickPrTaskPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var Needle = Strings["Needle"];
var PathSegment = Strings["PathSegment"];
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Needle, StringComparison.OrdinalIgnoreCase) && P.Url.Contains(PathSegment, StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { await Console.Error.WriteLineAsync($"no tab matching: {Needle} + {PathSegment}"); return 3; }
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Task.Delay(2000);

var ClickJs = $"() => {{ const linkRe = new RegExp('{Strings["LinkTextPattern"]}', 'i'); const links = Array.from(document.querySelectorAll('a, [role=link], [role=button]')); const target = links.find(l => linkRe.test(l.textContent || '')); if (!target) return 'NO_MATCHING_LINK'; target.click(); return 'CLICKED: ' + (target.textContent || target.href || '').slice(0, 100); }}";
var Escaped = ClickJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var ClickResult = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\"");
await Console.Out.WriteLineAsync($"click: {ClickResult.Trim()}");

await Task.Delay(4000);

var ReadJs = $"() => {{ const valueSubstring = '{Strings["ValueSubstring"]}'; const inputs = Array.from(document.querySelectorAll('input')); const values = inputs.map(i => (i.value || '').trim()).filter(v => v.length > 0); const matches = values.filter(v => v.includes(valueSubstring)); const uris = values.filter(v => /^https?:\\/\\//.test(v)); return JSON.stringify({{matches: matches, totalUris: uris.length, allUris: uris.slice(0, 20), url: location.href}}, null, 2); }}";
var EscapedRead = ReadJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedRead}\"");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpClickPrTaskPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
