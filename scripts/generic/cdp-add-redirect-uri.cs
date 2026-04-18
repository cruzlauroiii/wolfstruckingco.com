#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run scripts/cdp-add-redirect-uri.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { Console.Error.WriteLine($"specific not found: {SpecPath}"); return 2; }

var Body = File.ReadAllText(SpecPath);
var Strings = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (Match M in CdpAddRedirectUriPatterns.ConstString().Matches(Body)) { Strings[M.Groups[1].Value] = M.Groups[2].Value; }
foreach (var Required in new[] { "PageNeedle", "HeadingText", "UriValue", "SaveButtonText", "AddButtonRegex" })
{
    if (!Strings.ContainsKey(Required)) { Console.Error.WriteLine($"specific missing const string {Required}"); return 3; }
}

static string RunCdp(string Cdp, string Repo, string Command)
{
    var Psi = new ProcessStartInfo("dotnet", $"run \"{Cdp}\" -- {Command}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Repo,
    };
    using var Proc = Process.Start(Psi)!;
    var Out = Proc.StandardOutput.ReadToEnd();
    Proc.WaitForExit();
    return Out;
}

var PageList = RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages = new List<(int Idx, string Url)>();
foreach (Match M in CdpAddRedirectUriPatterns.PageLine().Matches(PageList)) { Pages.Add((int.Parse(M.Groups[1].Value), M.Groups[2].Value)); }
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["PageNeedle"], StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { Console.Error.WriteLine($"no tab matching: {Strings["PageNeedle"]}"); return 4; }
Console.WriteLine($"target page {HitIdx}: {HitUrl[..Math.Min(HitUrl.Length, 100)]}");

var ClickAddJs = $"() => {{ const headingText = '{Strings["HeadingText"]}'; const addRe = new RegExp('{Strings["AddButtonRegex"]}', 'i'); const all = Array.from(document.querySelectorAll('h1, h2, h3, h4, h5, label, span, div')); const heading = all.find(n => (n.textContent || '').trim() === headingText && n.children.length < 5); if (!heading) return JSON.stringify({{step: 'find-heading', error: 'NO_HEADING'}}); let scope = heading.parentElement; for (let depth = 0; depth < 8 && scope; depth++) {{ const buttons = Array.from(scope.querySelectorAll('button, [role=button], a')); const addBtn = buttons.find(b => addRe.test((b.textContent || '').trim())); if (addBtn) {{ addBtn.scrollIntoView({{block: 'center'}}); addBtn.click(); return JSON.stringify({{step: 'click-add', depth: depth, btnText: (addBtn.textContent || '').trim().slice(0, 60)}}); }} scope = scope.parentElement; }} return JSON.stringify({{step: 'click-add', error: 'NO_ADD_BUTTON'}}); }}";
var EscapedClick = ClickAddJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var ClickResult = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedClick}\" --pageId {HitIdx}");
Console.WriteLine($"click-add: {ClickResult.Trim()}");

System.Threading.Thread.Sleep(1000);

var FillJs = $"() => {{ const uriValue = '{Strings["UriValue"]}'; const inputs = Array.from(document.querySelectorAll('input[type=text], input[type=url], input:not([type])')); const empty = inputs.filter(i => !i.value && i.offsetParent !== null); if (empty.length === 0) return JSON.stringify({{step: 'fill', error: 'NO_EMPTY_INPUT'}}); const target = empty[0]; target.focus(); const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set; setter.call(target, uriValue); target.dispatchEvent(new Event('input', {{bubbles: true}})); target.dispatchEvent(new Event('change', {{bubbles: true}})); target.dispatchEvent(new Event('blur', {{bubbles: true}})); return JSON.stringify({{step: 'fill', value: target.value}}); }}";
var EscapedFill = FillJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var FillResult = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedFill}\" --pageId {HitIdx}");
Console.WriteLine($"fill: {FillResult.Trim()}");

System.Threading.Thread.Sleep(500);

var SaveJs = $"() => {{ const saveText = '{Strings["SaveButtonText"]}'; const buttons = Array.from(document.querySelectorAll('button, input[type=submit], a')).filter(b => b.offsetParent !== null); const saveBtn = buttons.find(b => ((b.innerText || b.value || '').trim()) === saveText); if (!saveBtn) return JSON.stringify({{step: 'save', error: 'NO_SAVE_BUTTON'}}); saveBtn.scrollIntoView({{block: 'center'}}); saveBtn.click(); return JSON.stringify({{step: 'save', btnTag: saveBtn.tagName}}); }}";
var EscapedSave = SaveJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var SaveResult = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedSave}\" --pageId {HitIdx}");
Console.WriteLine($"save: {SaveResult.Trim()}");
return 0;

namespace Scripts
{
    internal static partial class CdpAddRedirectUriPatterns
    {
        [GeneratedRegex("""const\s+string\s+(\w+)\s*=\s*@?"((?:[^"\\]|\\.)*)"\s*;""")]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
