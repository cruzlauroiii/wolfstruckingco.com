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
foreach (Match M in CdpProbeEntraSavePatterns.PageLine().Matches(List)) { Pages.Add((int.Parse(M.Groups[1].Value), M.Groups[2].Value)); }
var (HitIdx, _) = Pages.FirstOrDefault(P => P.Url.Contains("entra.microsoft.com", StringComparison.OrdinalIgnoreCase));
if (HitIdx == 0) { Console.Error.WriteLine("no entra tab"); return 1; }

var Js = "() => { const inputs = Array.from(document.querySelectorAll('input')).filter(i => i.offsetParent !== null).map(i => ({ type: i.type, value: (i.value || '').slice(0, 200), placeholder: i.placeholder || '', aria: i.getAttribute('aria-label') || '' })); const allInteractive = Array.from(document.querySelectorAll('button, [role=button], input[type=submit], input[type=button], a')).filter(b => b.offsetParent !== null).map(b => ({ tag: b.tagName, role: b.getAttribute('role') || '', text: (b.innerText || b.textContent || b.value || '').trim().slice(0, 80), aria: b.getAttribute('aria-label') || '', dataAuto: b.getAttribute('data-automation-id') || '', dataKey: b.getAttribute('data-bi-name') || '' })).filter(b => (b.text.length > 0 || b.aria.length > 0) && b.text.length < 100); return JSON.stringify({ visibleInputs: inputs, interactive: allInteractive }, null, 2); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx}");
Console.WriteLine(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpProbeEntraSavePatterns
    {
        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
