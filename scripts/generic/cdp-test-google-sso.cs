#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-test-google-sso.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpTestGoogleSsoPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
foreach (var Required in new[] { "LoginUrl", "LoginPageNeedle", "ProviderHrefNeedle", "SessionKey", "EmailKey", "RoleKey" })
{
    if (!Strings.ContainsKey(Required)) { await Console.Error.WriteLineAsync($"config missing const string {Required}"); return 3; }
}
var LoginUrl = Strings["LoginUrl"];

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

await Console.Out.WriteLineAsync($"step 1: opening {LoginUrl}");
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"new_page \"{LoginUrl}\"");
await Task.Delay(8000);

await Console.Out.WriteLineAsync("step 1b: select Login tab explicitly");
var List = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages = CdpTestGoogleSsoPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (LoginIdx, LoginUrlMatch) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["LoginPageNeedle"], StringComparison.OrdinalIgnoreCase));
if (LoginUrlMatch is null) { await Console.Error.WriteLineAsync("Login tab not found"); return 4; }
await Console.Out.WriteLineAsync($"  selected page {LoginIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {LoginUrlMatch}");
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {LoginIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Task.Delay(1500);

await Console.Out.WriteLineAsync("step 2: clicking provider SSO button");
var ClickJs = $"() => {{ const sessionKey = '{Strings["SessionKey"]}'; const emailKey = '{Strings["EmailKey"]}'; const roleKey = '{Strings["RoleKey"]}'; const hrefNeedle = '{Strings["ProviderHrefNeedle"]}'; try{{localStorage.removeItem(sessionKey);localStorage.removeItem(emailKey);localStorage.removeItem(roleKey);}}catch(e){{}} const links = Array.from(document.querySelectorAll('a[href]')); const a = links.find(e => (e.href || '').includes(hrefNeedle)); if (!a) {{ const all = links.map(l => l.href).slice(0,30); return 'NO_PROVIDER_BUTTON; links=' + JSON.stringify(all); }} a.click(); return 'CLICKED: ' + a.href; }}";
var EscapedClick = ClickJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var ClickResult = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedClick}\" --pageId {LoginIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync($"  {ClickResult.Trim()}");

await Console.Out.WriteLineAsync("step 3: waiting for OAuth round-trip (8s)");
await Task.Delay(8000);

await Console.Out.WriteLineAsync("step 4: reading current page");
var ReadJs = $"() => {{ const sessionKey = '{Strings["SessionKey"]}'; const emailKey = '{Strings["EmailKey"]}'; const roleKey = '{Strings["RoleKey"]}'; const url = location.href; const title = document.title; const headerText = (document.querySelector('header, [class*=TopBar], [class*=topbar], nav, [class*=Nav]') || {{innerText: ''}}).innerText || ''; const ls = {{sess: localStorage.getItem(sessionKey), email: localStorage.getItem(emailKey), role: localStorage.getItem(roleKey)}}; return JSON.stringify({{url: url, title: title, headerText: headerText.slice(0, 500), localStorage: ls}}, null, 2); }}";
var EscapedRead = ReadJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var State = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{EscapedRead}\" --pageId {LoginIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Console.Out.WriteLineAsync(State);
return 0;

namespace Scripts
{
    internal static partial class CdpTestGoogleSsoPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
