#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-add-js-origin.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpAddJsOriginPatterns.ConstString().Matches(Body)
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
var Pages = CdpAddJsOriginPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["Needle"], StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { await Console.Error.WriteLineAsync($"no edit-page tab matching: {Strings["Needle"]}"); return 3; }
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Task.Delay(1500);

var Js = $"() => {{ const headingText = '{Strings["SectionHeading"]}'; const all = Array.from(document.querySelectorAll('*')); const heading = all.find(n => n.tagName && /^(H1|H2|H3|H4|H5|H6|DIV|SPAN|LABEL)$/.test(n.tagName) && (n.textContent || '').trim() === headingText); if (!heading) return JSON.stringify({{step: 'find-heading', error: 'NO_HEADING'}}); let scope = heading.parentElement; for (let depth = 0; depth < 8 && scope; depth++) {{ const buttons = Array.from(scope.querySelectorAll('button, [role=button]')); const addBtn = buttons.find(b => /add\\s*uri/i.test(b.textContent || '')); if (addBtn) {{ addBtn.scrollIntoView({{block: 'center'}}); addBtn.click(); return JSON.stringify({{step: 'clicked', depth: depth, btnText: (addBtn.textContent || '').trim().slice(0, 60)}}); }} scope = scope.parentElement; }} return JSON.stringify({{step: 'find-button', error: 'NO_ADD_URI_NEAR_JS_ORIGINS'}}); }}";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\"");
await Console.Out.WriteLineAsync($"click step: {Result.Trim()}");

await Task.Delay(800);

var ReadJs = "() => { const inputs = Array.from(document.querySelectorAll('input[type=text], input[type=url], input:not([type])')); const empty = inputs.find(i => !i.value && i.placeholder); if (empty) { empty.focus(); empty.scrollIntoView({block: 'center'}); } const focused = document.activeElement; const focusInfo = focused && focused.tagName === 'INPUT' ? {placeholder: focused.placeholder || '', name: focused.name || focused.id || '?'} : 'NONE'; const allFilled = inputs.map((i, idx) => ({idx: idx, val: (i.value || '').trim().slice(0, 60), focused: i === document.activeElement})).filter(o => o.val || o.focused); return JSON.stringify({focusedInput: focusInfo, allFields: allFilled}, null, 2); }";
var EscapedRead = ReadJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var State = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedRead}\"");
await Console.Out.WriteLineAsync();
await Console.Out.WriteLineAsync("page state after click:");
await Console.Out.WriteLineAsync(State);
return 0;

namespace Scripts
{
    internal static partial class CdpAddJsOriginPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
