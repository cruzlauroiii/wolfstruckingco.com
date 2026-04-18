#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-fix-google-uri.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpFixGoogleUriPatterns.ConstString().Matches(Body)
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
var Pages = CdpFixGoogleUriPatterns.PageLine().Matches(PageList)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var Needle = Strings["Needle"];
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Needle, StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { await Console.Error.WriteLineAsync($"no tab matching: {Needle}"); return 3; }
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

var Js = $"() => {{ const headingRe = new RegExp('{Strings["HeadingRegex"]}', 'i'); const addRe = new RegExp('{Strings["AddButtonRegex"]}', 'i'); const all = Array.from(document.querySelectorAll('*')); const heading = all.find(n => n.tagName && /^(H1|H2|H3|H4|H5|DIV|SPAN|LABEL)$/.test(n.tagName) && headingRe.test((n.textContent || '').trim()) && (n.textContent || '').length < 200); if (!heading) return 'NO_HEADING_FOUND'; let scope = heading.parentElement; for (let depth = 0; depth < 6 && scope; depth++) {{ const buttons = Array.from(scope.querySelectorAll('button, [role=button]')); const addBtn = buttons.find(b => addRe.test(b.textContent || '')); if (addBtn) {{ addBtn.scrollIntoView({{block: 'center'}}); addBtn.click(); return 'CLICKED at depth ' + depth + ': ' + (addBtn.textContent || '').trim().slice(0,80); }} scope = scope.parentElement; }} return 'NO_ADD_BUTTON_NEAR_HEADING'; }}";
var EscapedJs = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Click = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedJs}\"");
await Console.Out.WriteLineAsync($"click: {Click}");

await Task.Delay(800);

var ReadJs = "() => { const inputs = Array.from(document.querySelectorAll('input')); const values = inputs.map((i, idx) => ({i: idx, t: i.type, v: (i.value || '').trim(), p: (i.placeholder || '').trim(), focused: i === document.activeElement})).filter(o => o.v || o.focused); return JSON.stringify(values, null, 2); }";
var EscapedRead = ReadJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var State = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedRead}\"");
await Console.Out.WriteLineAsync();
await Console.Out.WriteLineAsync("page state (focused + filled inputs):");
await Console.Out.WriteLineAsync(State);
return 0;

namespace Scripts
{
    internal static partial class CdpFixGoogleUriPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
