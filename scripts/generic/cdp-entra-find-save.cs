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
foreach (Match M in CdpEntraFindSavePatterns.PageLine().Matches(List)) { Pages.Add((int.Parse(M.Groups[1].Value), M.Groups[2].Value)); }
var (HitIdx, _) = Pages.FirstOrDefault(P => P.Url.Contains("entra.microsoft.com", StringComparison.OrdinalIgnoreCase));
if (HitIdx == 0) { Console.Error.WriteLine("no entra tab"); return 1; }

var Js = "() => { const all = Array.from(document.querySelectorAll('button, [role=button], input[type=submit]')).filter(b => b.offsetParent !== null); const matches = all.map(b => ({ tag: b.tagName, role: b.getAttribute('role') || '', text: (b.innerText || b.textContent || b.value || '').trim().slice(0, 80), aria: b.getAttribute('aria-label') || '', disabled: !!b.disabled, dataAuto: b.getAttribute('data-automation-id') || '', dataKey: b.getAttribute('data-bi-name') || '' })).filter(b => /save|publish|apply|commit|done/i.test(b.text + b.aria + b.dataAuto + b.dataKey)); const banners = Array.from(document.querySelectorAll('[role=alert], [class*=banner], [class*=Banner], [class*=notification], [class*=Notification]')).filter(b => b.offsetParent !== null).map(b => (b.innerText || '').trim().slice(0, 200)).filter(t => t.length > 0); const allBtnText = all.map(b => ((b.innerText || b.textContent || '').trim()).slice(0, 50)).filter(t => t.length > 0 && t.length < 50); return JSON.stringify({ saveCandidates: matches, banners: banners, sampleAllText: allBtnText.slice(0, 50) }, null, 2); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = RunCdp(Cdp, Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx}");
Console.WriteLine(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpEntraFindSavePatterns
    {
        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
