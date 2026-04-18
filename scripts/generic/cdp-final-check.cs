#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-final-check.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpFinalCheckPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
if (!Strings.TryGetValue("Needle", out var Needle)) { await Console.Error.WriteLineAsync("config missing const string Needle"); return 3; }

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

await Task.Delay(5000);

var List = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
await Console.Out.WriteLineAsync(List);

var Pages = CdpFinalCheckPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Needle, StringComparison.OrdinalIgnoreCase));
if (HitUrl is null) { await Console.Error.WriteLineAsync($"no tab matching: {Needle}"); return 4; }
await Console.Out.WriteLineAsync($"selected page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {HitUrl}");
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

var Js = $"() => {{ const ls = {{sess: localStorage.getItem('{Strings["SessionKey"]}'), email: localStorage.getItem('{Strings["EmailKey"]}'), role: localStorage.getItem('{Strings["RoleKey"]}')}}; const url = location.href; const headerEl = document.querySelector('header, [class*=TopBar], [class*=topbar], nav'); const headerText = headerEl ? headerEl.innerText.slice(0, 400) : ''; const signinAnchor = Array.from(document.querySelectorAll('a, button')).find(e => /sign\\s*in/i.test(e.textContent || '')); const hasSignIn = signinAnchor ? signinAnchor.textContent.trim() : 'NONE'; return JSON.stringify({{url: url, headerText: headerText, signInVisible: hasSignIn, localStorage: ls}}, null, 2); }}";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\"");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpFinalCheckPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
