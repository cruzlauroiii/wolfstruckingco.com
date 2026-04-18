#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run scripts/cdp-probe-entra-deep.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { Console.Error.WriteLine($"specific not found: {SpecPath}"); return 2; }

var Body = File.ReadAllText(SpecPath);
var Strings = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (Match M in CdpProbeEntraDeepPatterns.ConstString().Matches(Body)) { Strings[M.Groups[1].Value] = M.Groups[2].Value; }

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
foreach (Match M in CdpProbeEntraDeepPatterns.PageLine().Matches(List)) { Pages.Add((int.Parse(M.Groups[1].Value), M.Groups[2].Value)); }
var (HitIdx, _) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["Needle"], StringComparison.OrdinalIgnoreCase));
if (HitIdx == 0) { var Nd = Strings["Needle"]; Console.Error.WriteLine($"no tab matching: {Nd}"); return 4; }

var Js = "() => { const findText = (txt) => { const all = Array.from(document.querySelectorAll('button, [role=button], a, span, div, h1, h2, h3, h4, h5, label')); return all.filter(e => (e.textContent || '').trim() === txt && e.offsetParent !== null); }; const addBtns = findText('Add Redirect URI'); const cfgBtns = findText('Redirect URI configuration'); const allButtons = Array.from(document.querySelectorAll('button, [role=button]')).filter(b => b.offsetParent !== null).map(b => ({ text: (b.textContent || '').trim().slice(0, 80), aria: b.getAttribute('aria-label') || '', tag: b.tagName, role: b.getAttribute('role') || '' })).filter(b => b.text.length > 0 && b.text.length < 80); const allLinks = Array.from(document.querySelectorAll('a')).filter(a => a.offsetParent !== null).map(a => ((a.textContent || '').trim()).slice(0, 80)).filter(t => t.length > 0 && t.length < 80); return JSON.stringify({ addRedirectUriElements: addBtns.map(e => ({ tag: e.tagName, role: e.getAttribute('role'), clickable: !!e.onclick || e.tagName === 'BUTTON' || e.getAttribute('role') === 'button' })), cfgElements: cfgBtns.length, allButtons: allButtons.slice(0, 80), allLinks: allLinks.slice(0, 30) }, null, 2); }";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\" --pageId {HitIdx}");
Console.WriteLine(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpProbeEntraDeepPatterns
    {
        [GeneratedRegex("""const\s+string\s+(\w+)\s*=\s*@?"((?:[^"\\]|\\.)*)"\s*;""")]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
