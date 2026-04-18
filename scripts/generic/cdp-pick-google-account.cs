#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-pick-google-account.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpPickGoogleAccountPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
foreach (var Required in new[] { "AccountEmail", "ProviderTabNeedle", "SuccessNeedle1", "SuccessNeedle2", "SessionKey", "EmailKey", "RoleKey" })
{
    if (!Strings.ContainsKey(Required)) { await Console.Error.WriteLineAsync($"config missing const string {Required}"); return 3; }
}
var Email = Strings["AccountEmail"];

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

var List = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages = CdpPickGoogleAccountPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (GoogleIdx, GoogleUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["ProviderTabNeedle"], StringComparison.OrdinalIgnoreCase));
if (GoogleUrl is null) { await Console.Error.WriteLineAsync("no provider sign-in tab"); return 4; }
await Console.Out.WriteLineAsync($"using page {GoogleIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {GoogleUrl[..Math.Min(GoogleUrl.Length, 100)]}...");

var Js = $"() => {{ const all = Array.from(document.querySelectorAll('*')); const target = all.find(el => (el.textContent || '').trim() === '{Email}'); if (!target) return 'NO_ACCOUNT_TILE'; let clickable = target; for (let i = 0; i < 6 && clickable; i++) {{ if (clickable.getAttribute && (clickable.getAttribute('role') === 'link' || clickable.tagName === 'A' || clickable.tagName === 'BUTTON' || clickable.getAttribute('data-identifier'))) {{ clickable.click(); return 'CLICKED at depth ' + i + ': ' + (clickable.textContent || '').slice(0, 80); }} clickable = clickable.parentElement; }} target.click(); return 'CLICKED target itself'; }}";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var ClickResult = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\" --pageId {GoogleIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync($"click: {ClickResult.Trim()}");

await Task.Delay(8000);

var List2 = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages2 = CdpPickGoogleAccountPatterns.PageLine().Matches(List2)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (FinalIdx, FinalUrl) = Pages2.FirstOrDefault(P => P.Url.Contains(Strings["SuccessNeedle1"], StringComparison.OrdinalIgnoreCase) || P.Url.Contains(Strings["SuccessNeedle2"], StringComparison.OrdinalIgnoreCase));
if (FinalUrl is null) { FinalIdx = GoogleIdx; FinalUrl = GoogleUrl; }
await Console.Out.WriteLineAsync($"reading page {FinalIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {FinalUrl[..Math.Min(FinalUrl.Length, 100)]}...");

var ReadJs = $"() => {{ const sessionKey = '{Strings["SessionKey"]}'; const emailKey = '{Strings["EmailKey"]}'; const roleKey = '{Strings["RoleKey"]}'; const url = location.href; const title = document.title; const ls = {{sess: localStorage.getItem(sessionKey), email: localStorage.getItem(emailKey), role: localStorage.getItem(roleKey)}}; return JSON.stringify({{url: url, title: title, localStorage: ls}}, null, 2); }}";
var EscapedRead = ReadJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var State = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedRead}\" --pageId {FinalIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync(State);
return 0;

namespace Scripts
{
    internal static partial class CdpPickGoogleAccountPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
