#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

const string UriValue = "https://wolfstruckingco.nbth.workers.dev/oauth/microsoft/callback";

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

var List = RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages = new List<(int Idx, string Url)>();
foreach (Match M in CdpEntraAddUriV2Patterns.PageLine().Matches(List)) { Pages.Add((int.Parse(M.Groups[1].Value), M.Groups[2].Value)); }
var (HitIdx, _) = Pages.FirstOrDefault(P => P.Url.Contains("entra.microsoft.com", StringComparison.OrdinalIgnoreCase));
if (HitIdx == 0) { Console.Error.WriteLine("no entra tab"); return 1; }
Console.WriteLine($"target page {HitIdx}");

// Step 1: Click "Add Redirect URI" - SCOPED to the cfg heading element so we don't hit the search box
var ClickAddJs = "() => { const all = Array.from(document.querySelectorAll('h1, h2, h3, h4, h5, label, span, div')); const heading = all.find(n => (n.textContent || '').trim() === 'Redirect URI configuration' && n.children.length < 5); if (!heading) return JSON.stringify({ step: 'click-add', error: 'NO_HEADING' }); let scope = heading.parentElement; for (let depth = 0; depth < 8 && scope; depth++) { const buttons = Array.from(scope.querySelectorAll('button, [role=button], a')); const addBtn = buttons.find(b => /Add\\s+Redirect\\s+URI/i.test((b.textContent || '').trim())); if (addBtn) { addBtn.scrollIntoView({ block: 'center' }); addBtn.click(); return JSON.stringify({ step: 'click-add', depth: depth, btnText: (addBtn.textContent || '').trim().slice(0, 60) }); } scope = scope.parentElement; } return JSON.stringify({ step: 'click-add', error: 'NO_ADD_BUTTON' }); }";
var ClickResult = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{ClickAddJs.Replace("\"", "\\\"", StringComparison.Ordinal)}\" --pageId {HitIdx}");
Console.WriteLine($"step1 click-add: {ClickResult.Trim()}");
System.Threading.Thread.Sleep(1500);

// Step 2: Probe the flyout - see what platform options exist
var ProbeFlyoutJs = "() => { const allInteractive = Array.from(document.querySelectorAll('button, [role=button], div[tabindex], li')).filter(b => b.offsetParent !== null).map(b => ({ tag: b.tagName, role: b.getAttribute('role') || '', text: (b.textContent || '').trim().slice(0, 100), aria: b.getAttribute('aria-label') || '' })).filter(b => b.text.length > 0 && b.text.length < 100); return JSON.stringify({ interactive: allInteractive.slice(0, 60) }, null, 2); }";
var FlyoutResult = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{ProbeFlyoutJs.Replace("\"", "\\\"", StringComparison.Ordinal)}\" --pageId {HitIdx}");
Console.WriteLine($"step2 flyout-probe:");
Console.WriteLine(FlyoutResult);
System.Threading.Thread.Sleep(500);

// Step 3: Click "Web" platform option (worker uses confidential client = web)
var ClickWebJs = "() => { const all = Array.from(document.querySelectorAll('button, [role=button], div, li, span')).filter(b => b.offsetParent !== null); const webOpts = all.filter(e => { const t = (e.textContent || '').trim(); return t === 'Web' || /^Web\\s/.test(t); }); if (webOpts.length === 0) return JSON.stringify({ step: 'click-web', error: 'NO_WEB_OPTION', sampleTexts: all.map(e => (e.textContent || '').trim().slice(0, 40)).filter(t => t.length > 0).slice(0, 30) }); const target = webOpts.find(o => o.tagName === 'BUTTON' || o.getAttribute('role') === 'button' || o.tagName === 'LI') || webOpts[0]; target.scrollIntoView({ block: 'center' }); target.click(); return JSON.stringify({ step: 'click-web', tag: target.tagName, role: target.getAttribute('role'), text: (target.textContent || '').trim().slice(0, 60) }); }";
var WebClick = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{ClickWebJs.Replace("\"", "\\\"", StringComparison.Ordinal)}\" --pageId {HitIdx}");
Console.WriteLine($"step3 click-web: {WebClick.Trim()}");
System.Threading.Thread.Sleep(1500);

// Step 4: Now find the URI input (NOT the search box) and fill it
var FillJs = $"() => {{ const inputs = Array.from(document.querySelectorAll('input[type=text], input[type=url], input:not([type])')); const candidates = inputs.filter(i => i.offsetParent !== null && !(i.placeholder || '').includes('Search')); if (candidates.length === 0) return JSON.stringify({{ step: 'fill', error: 'NO_URI_INPUT', allVisible: inputs.filter(i => i.offsetParent !== null).map(i => ({{ type: i.type, placeholder: i.placeholder, aria: i.getAttribute('aria-label') }})) }}); const target = candidates[0]; target.focus(); const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set; setter.call(target, '{UriValue}'); target.dispatchEvent(new Event('input', {{ bubbles: true }})); target.dispatchEvent(new Event('change', {{ bubbles: true }})); target.dispatchEvent(new Event('blur', {{ bubbles: true }})); return JSON.stringify({{ step: 'fill', value: target.value, placeholder: target.placeholder, aria: target.getAttribute('aria-label') }}); }}";
var FillResult = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{FillJs.Replace("\"", "\\\"", StringComparison.Ordinal)}\" --pageId {HitIdx}");
Console.WriteLine($"step4 fill: {FillResult.Trim()}");
System.Threading.Thread.Sleep(800);

// Step 5: Click Save / Configure / Apply
var SaveJs = "() => { const candidates = Array.from(document.querySelectorAll('button, [role=button], input[type=submit]')).filter(b => b.offsetParent !== null); const labelled = candidates.map(b => ({ el: b, text: (b.innerText || b.textContent || b.value || '').trim() })); const matchTexts = ['Save', 'Configure', 'Add', 'Apply', 'Submit', 'Done']; for (const want of matchTexts) { const hit = labelled.find(l => l.text === want); if (hit) { hit.el.scrollIntoView({ block: 'center' }); hit.el.click(); return JSON.stringify({ step: 'save', clicked: want }); } } return JSON.stringify({ step: 'save', error: 'NO_SAVE_BUTTON', allTexts: labelled.map(l => l.text).filter(t => t.length > 0 && t.length < 50) }); }";
var SaveResult = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{SaveJs.Replace("\"", "\\\"", StringComparison.Ordinal)}\" --pageId {HitIdx}");
Console.WriteLine($"step5 save: {SaveResult.Trim()}");
return 0;

namespace Scripts
{
    internal static partial class CdpEntraAddUriV2Patterns
    {
        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
