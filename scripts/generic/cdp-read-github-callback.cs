#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-read-github-callback.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpReadGithubCallbackPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
foreach (var Required in new[] { "PageNeedle", "AppNamePattern" })
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
var Pages = CdpReadGithubCallbackPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["PageNeedle"], StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { await Console.Error.WriteLineAsync($"no tab matching: {Strings["PageNeedle"]}"); return 4; }
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Task.Delay(1500);

var ClickJs = $"() => {{ const appNameRe = new RegExp('{Strings["AppNamePattern"]}', 'i'); const links = Array.from(document.querySelectorAll('a')); const target = links.find(l => (l.href || '').match(/\\/settings\\/applications\\/\\d+$/) || (l.href || '').match(/\\/developers\\/applications\\/\\d+$/) || ((l.href || '').includes('/applications/') && appNameRe.test(l.textContent || ''))); if (!target) {{ const sample = links.filter(l => (l.href || '').includes('/applications/')).map(l => ({{href: l.href, text: (l.textContent || '').trim().slice(0,80)}})); return 'NO_OAUTH_APP_LINK; appLinks=' + JSON.stringify(sample); }} target.click(); return 'CLICKED: ' + target.href; }}";
var Escaped = ClickJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var ClickResult = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync($"click: {ClickResult.Trim()}");

await Task.Delay(4000);

var ReadJs = "() => { const inputs = Array.from(document.querySelectorAll('input')); const callback = inputs.find(i => /callback/i.test(i.name || '') || /callback/i.test(i.id || '')); const homepage = inputs.find(i => /url/i.test(i.name || '') && !/callback/i.test(i.name || '')); const nameInput = inputs.find(i => /name$/i.test(i.name || '')); const all = inputs.map(i => ({name: i.name || i.id || '?', val: (i.value || '').trim()})).filter(o => o.val); return JSON.stringify({title: document.title, url: location.href, callbackInputName: callback ? (callback.name || callback.id) : 'NONE', callbackValue: callback ? callback.value : '', homepageValue: homepage ? homepage.value : '', appName: nameInput ? nameInput.value : '', allInputs: all.slice(0, 20)}, null, 2); }";
var EscapedRead = ReadJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedRead}\" --pageId {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpReadGithubCallbackPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
