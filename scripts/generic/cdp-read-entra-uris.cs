#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run scripts/cdp-read-entra-uris.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { Console.Error.WriteLine($"specific not found: {SpecPath}"); return 2; }

var Body = File.ReadAllText(SpecPath);
var Strings = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (Match M in CdpReadEntraUrisPatterns.ConstString().Matches(Body)) { Strings[M.Groups[1].Value] = M.Groups[2].Value; }
foreach (var Required in new[] { "Needle", "ValueSubstring" })
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
foreach (Match M in CdpReadEntraUrisPatterns.PageLine().Matches(PageList)) { Pages.Add((int.Parse(M.Groups[1].Value), M.Groups[2].Value)); }
var Needle = Strings["Needle"];
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Needle, StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { Console.Error.WriteLine($"no tab matching: {Needle}"); return 4; }
Console.WriteLine($"target page {HitIdx}: {HitUrl[..Math.Min(HitUrl.Length, 120)]}");
_ = RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx}");

var Js = $"() => {{ const valueSubstring = '{Strings["ValueSubstring"]}'; const inputs = Array.from(document.querySelectorAll('input')); const values = inputs.map(i => (i.value || '').trim()).filter(v => v.length > 0); const matches = values.filter(v => v.includes(valueSubstring)); const allUris = values.filter(v => /^https?:\\/\\//.test(v)); const headings = Array.from(document.querySelectorAll('h1, h2, h3, h4, h5')).map(h => (h.textContent || '').trim()).filter(t => t.length > 0 && t.length < 80); return JSON.stringify({{matches: matches, allUris: allUris.slice(0, 30), headings: headings.slice(0, 30)}}, null, 2); }}";
var EscapedJs = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedJs}\" --pageId {HitIdx}");
Console.WriteLine(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpReadEntraUrisPatterns
    {
        [GeneratedRegex("""const\s+string\s+(\w+)\s*=\s*@?"((?:[^"\\]|\\.)*)"\s*;""")]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
