#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-create-github-oauth-app.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpCreateGithubOauthAppPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
foreach (var Required in new[] { "FormUrl", "PageNeedle", "AppName", "HomepageUrl", "AppDescription", "CallbackUrl", "SubmitButtonText" })
{
    if (!Strings.ContainsKey(Required)) { await Console.Error.WriteLineAsync($"specific missing const string {Required}"); return 3; }
}

static async Task<string> RunCdp(string Command)
{
    var Psi = new ProcessStartInfo("dotnet", $"run \"{Paths.Cdp}\" -- {Command}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Paths.Repo,
    };
    using var Proc = Process.Start(Psi)!;
    var Out = await Proc.StandardOutput.ReadToEndAsync();
    await Proc.WaitForExitAsync();
    return Out;
}

var List = await RunCdp("list_pages");
var Pages = CdpCreateGithubOauthAppPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["PageNeedle"], StringComparison.OrdinalIgnoreCase));
if (HitUrl is null)
{
    await Console.Out.WriteLineAsync($"opening fresh tab for {Strings["FormUrl"]}");
    _ = await RunCdp($"new_page \"{Strings["FormUrl"]}\"");
    await Task.Delay(4000);
    var List2 = await RunCdp("list_pages");
    Pages = [.. CdpCreateGithubOauthAppPatterns.PageLine().Matches(List2)
        .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))];
    (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["PageNeedle"], StringComparison.OrdinalIgnoreCase));
    if (HitUrl is null) { await Console.Error.WriteLineAsync($"failed to open {Strings["FormUrl"]}"); return 4; }
}
await Console.Out.WriteLineAsync($"target page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {HitUrl[..Math.Min(HitUrl.Length, 120)]}");
_ = await RunCdp($"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Task.Delay(1500);

var FillJs = $"() => {{ const setVal = (sel, val) => {{ const el = document.querySelector(sel); if (!el) return false; el.focus(); const setter = Object.getOwnPropertyDescriptor(el.tagName === 'TEXTAREA' ? window.HTMLTextAreaElement.prototype : window.HTMLInputElement.prototype, 'value').set; setter.call(el, val); el.dispatchEvent(new Event('input', {{bubbles: true}})); el.dispatchEvent(new Event('change', {{bubbles: true}})); el.dispatchEvent(new Event('blur', {{bubbles: true}})); return el.value === val; }}; const r = {{name: setVal('input[name=\"oauth_application[name]\"]', '{Strings["AppName"]}'), homepage: setVal('input[name=\"oauth_application[url]\"]', '{Strings["HomepageUrl"]}'), description: setVal('textarea[name=\"oauth_application[description]\"]', '{Strings["AppDescription"]}'), callback: setVal('input[name=\"oauth_application[callback_url]\"]', '{Strings["CallbackUrl"]}')}}; return JSON.stringify(r); }}";
var EscapedFill = FillJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var FillResult = await RunCdp($"evaluate_script \"{EscapedFill}\" --pageId {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync($"fill: {FillResult.Trim()}");

await Task.Delay(1000);

var SubmitJs = $"() => {{ const submitText = '{Strings["SubmitButtonText"]}'; const btns = Array.from(document.querySelectorAll('button[type=submit], input[type=submit]')).filter(b => b.offsetParent !== null); const target = btns.find(b => ((b.innerText || b.value || '').trim()) === submitText); if (!target) {{ const sample = btns.map(b => (b.innerText || b.value || '').trim()).slice(0,10); return JSON.stringify({{step: 'submit', error: 'NO_SUBMIT', candidates: sample}}); }} target.scrollIntoView({{block: 'center'}}); target.click(); return JSON.stringify({{step: 'submit', clicked: true, text: (target.innerText || target.value || '').trim()}}); }}";
var EscapedSubmit = SubmitJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var SubmitResult = await RunCdp($"evaluate_script \"{EscapedSubmit}\" --pageId {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync($"submit: {SubmitResult.Trim()}");

await Task.Delay(5000);

var ReadJs = "() => { const codes = Array.from(document.querySelectorAll('code, .copyable-text, [data-copyable-text]')).map(c => (c.innerText || c.textContent || '').trim()).filter(t => t.length); const inputs = Array.from(document.querySelectorAll('input[readonly], input[disabled]')).map(i => ({name: i.name || i.id || '?', val: (i.value || '').trim()})); const headers = Array.from(document.querySelectorAll('h1, h2, h3, h4, dt')).map(h => (h.innerText || '').trim()).filter(t => t.length).slice(0, 30); const url = location.href; return JSON.stringify({title: document.title, url, codes: codes.slice(0, 20), inputs: inputs.slice(0, 20), headers}, null, 2); }";
var EscapedRead = ReadJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp($"evaluate_script \"{EscapedRead}\" --pageId {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync("=== POST-CREATE PAGE STATE ===");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpCreateGithubOauthAppPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
