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
foreach (Match M in CdpEntraClickConfigurePatterns.PageLine().Matches(List)) { Pages.Add((int.Parse(M.Groups[1].Value), M.Groups[2].Value)); }
var (HitIdx, _) = Pages.FirstOrDefault(P => P.Url.Contains("entra.microsoft.com", StringComparison.OrdinalIgnoreCase));
if (HitIdx == 0) { Console.Error.WriteLine("no entra tab"); return 1; }

var Js = "() => { const all = Array.from(document.querySelectorAll('button, [role=button], input[type=submit]')).filter(b => b.offsetParent !== null); const cfgs = all.filter(b => ((b.innerText || b.textContent || b.value || '').trim()) === 'Configure'); if (cfgs.length === 0) return JSON.stringify({ error: 'NO_CONFIGURE_BTN' }); const details = cfgs.map(c => ({ tag: c.tagName, role: c.getAttribute('role') || '', disabled: !!c.disabled, dataAuto: c.getAttribute('data-automation-id') || '', classList: Array.from(c.classList).slice(0, 5) })); cfgs[0].scrollIntoView({ block: 'center' }); cfgs[0].click(); return JSON.stringify({ clicked: true, count: cfgs.length, details: details }); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = RunCdp(Cdp, Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx}");
Console.WriteLine(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpEntraClickConfigurePatterns
    {
        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
