#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

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
foreach (Match M in CdpCloseEntraFlyoutPatterns.PageLine().Matches(List)) { Pages.Add((int.Parse(M.Groups[1].Value), M.Groups[2].Value)); }
var (HitIdx, _) = Pages.FirstOrDefault(P => P.Url.Contains("entra.microsoft.com", StringComparison.OrdinalIgnoreCase));
if (HitIdx == 0) { Console.Error.WriteLine("no entra tab"); return 1; }

var Js = "() => { const inputs = Array.from(document.querySelectorAll('input')).filter(i => i.offsetParent !== null); const target = inputs.find(i => (i.value || '').includes('wolfstruckingco')); if (target) { const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set; setter.call(target, ''); target.dispatchEvent(new Event('input', { bubbles: true })); target.dispatchEvent(new Event('change', { bubbles: true })); } const cancelBtns = Array.from(document.querySelectorAll('button, [role=button]')).filter(b => b.offsetParent !== null && (b.textContent || '').trim() === 'Cancel'); if (cancelBtns.length > 0) { cancelBtns[0].click(); return JSON.stringify({ cleared: !!target, cancelClicked: true }); } return JSON.stringify({ cleared: !!target, cancelClicked: false }); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx}");
Console.WriteLine(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpCloseEntraFlyoutPatterns
    {
        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
