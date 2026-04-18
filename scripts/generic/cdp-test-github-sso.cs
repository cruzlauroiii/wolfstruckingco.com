#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { Console.Error.WriteLine("usage: dotnet run scripts/cdp-test-github-sso.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { Console.Error.WriteLine($"specific not found: {SpecPath}"); return 2; }

var Body = File.ReadAllText(SpecPath);
var Strings = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (Match M in CdpTestGithubSsoPatterns.ConstString().Matches(Body)) { Strings[M.Groups[1].Value] = M.Groups[2].Value; }
foreach (var Required in new[] { "LoginUrl", "LoginPageNeedle", "ProviderHrefNeedle", "SessionKey", "EmailKey", "RoleKey" })
{
    if (!Strings.ContainsKey(Required)) { Console.Error.WriteLine($"config missing const string {Required}"); return 3; }
}
var LoginUrl = Strings["LoginUrl"];

static string RunCdp(string Command)
{
    var Psi = new ProcessStartInfo("dotnet", $"run \"{Paths.Cdp}\" -- {Command}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Paths.Repo,
    };
    using var Proc = Process.Start(Psi)!;
    var Out = Proc.StandardOutput.ReadToEnd();
    Proc.WaitForExit();
    return Out;
}

Console.WriteLine($"step 1: opening {LoginUrl}");
_ = RunCdp($"new_page \"{LoginUrl}\"");
System.Threading.Thread.Sleep(8000);

Console.WriteLine("step 1b: select Login tab explicitly");
var List = RunCdp("list_pages");
var Pages = new List<(int Idx, string Url)>();
foreach (Match M in CdpTestGithubSsoPatterns.PageLine().Matches(List)) { Pages.Add((int.Parse(M.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), M.Groups[2].Value)); }
var (LoginIdx, LoginUrlMatch) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["LoginPageNeedle"], StringComparison.OrdinalIgnoreCase));
if (LoginUrlMatch is null) { Console.Error.WriteLine("Login tab not found"); return 4; }
Console.WriteLine($"  selected page {LoginIdx}: {LoginUrlMatch}");
_ = RunCdp($"select_page {LoginIdx}");
System.Threading.Thread.Sleep(1500);

Console.WriteLine("step 2: clicking provider SSO button");
var ClickJs = $"() => {{ const sessionKey = '{Strings["SessionKey"]}'; const emailKey = '{Strings["EmailKey"]}'; const roleKey = '{Strings["RoleKey"]}'; const hrefNeedle = '{Strings["ProviderHrefNeedle"]}'; try{{localStorage.removeItem(sessionKey);localStorage.removeItem(emailKey);localStorage.removeItem(roleKey);}}catch(e){{}} const links = Array.from(document.querySelectorAll('a[href]')); const a = links.find(e => (e.href || '').includes(hrefNeedle)); if (!a) {{ const all = links.map(l => l.href).slice(0,30); return 'NO_PROVIDER_BUTTON; links=' + JSON.stringify(all); }} a.click(); return 'CLICKED: ' + a.href; }}";
var EscapedClick = ClickJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var ClickResult = RunCdp($"evaluate_script \"{EscapedClick}\" --pageId {LoginIdx}");
Console.WriteLine($"  {ClickResult.Trim()}");

Console.WriteLine("step 3: waiting for OAuth round-trip (10s)");
System.Threading.Thread.Sleep(10000);

Console.WriteLine("step 4: reading current page");
var ReadJs = $"() => {{ const sessionKey = '{Strings["SessionKey"]}'; const emailKey = '{Strings["EmailKey"]}'; const roleKey = '{Strings["RoleKey"]}'; const url = location.href; const title = document.title; const bodyText = (document.body ? document.body.innerText : '').slice(0, 800); const ls = {{sess: localStorage.getItem(sessionKey), email: localStorage.getItem(emailKey), role: localStorage.getItem(roleKey)}}; return JSON.stringify({{url: url, title: title, bodyText: bodyText, localStorage: ls}}, null, 2); }}";
var EscapedRead = ReadJs.Replace("\"", "\\\"", StringComparison.Ordinal);
var State = RunCdp($"evaluate_script \"{EscapedRead}\" --pageId {LoginIdx}");
Console.WriteLine(State);
return 0;

namespace Scripts
{
    internal static partial class CdpTestGithubSsoPatterns
    {
        [GeneratedRegex("""const\s+string\s+(\w+)\s*=\s*@?"((?:[^"\\]|\\.)*)"\s*;""")]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(\d+):\s+(\S+)", RegexOptions.Multiline)]
        internal static partial Regex PageLine();
    }
}
