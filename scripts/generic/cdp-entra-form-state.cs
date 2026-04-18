#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
const string Cdp = @"C:\repo\public\wolfstruckingco.com\main\scripts\chrome-devtools.cs";

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

var List = RunCdp(Cdp, Repo, "list_pages");
var Pages = new List<(int Idx, string Url)>();
foreach (Match M in CdpEntraFormStatePatterns.PageLine().Matches(List)) { Pages.Add((int.Parse(M.Groups[1].Value), M.Groups[2].Value)); }
var (HitIdx, _) = Pages.FirstOrDefault(P => P.Url.Contains("entra.microsoft.com", StringComparison.OrdinalIgnoreCase));
if (HitIdx == 0) { Console.Error.WriteLine("no entra tab"); return 1; }

var Js = "() => { const inputs = Array.from(document.querySelectorAll('input')).filter(i => i.offsetParent !== null).map(i => ({ type: i.type, value: (i.value || '').slice(0, 200), placeholder: i.placeholder || '', aria: i.getAttribute('aria-label') || '', validity: i.validity ? i.validity.valid : null, validationMessage: i.validationMessage || '' })); const cfgBtn = Array.from(document.querySelectorAll('button, [role=button]')).filter(b => b.offsetParent !== null && (b.innerText || b.textContent || '').trim() === 'Configure')[0]; const cfgInfo = cfgBtn ? { disabled: !!cfgBtn.disabled, ariaDisabled: cfgBtn.getAttribute('aria-disabled'), classList: Array.from(cfgBtn.classList) } : null; const errors = Array.from(document.querySelectorAll('[role=alert], [class*=Error], [class*=error]')).filter(e => e.offsetParent !== null).map(e => (e.innerText || e.textContent || '').trim()).filter(t => t.length > 0 && t.length < 300); return JSON.stringify({ inputs: inputs, configureBtn: cfgInfo, errors: errors }, null, 2); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = RunCdp(Cdp, Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx}");
Console.WriteLine(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpEntraFormStatePatterns
    {
        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
