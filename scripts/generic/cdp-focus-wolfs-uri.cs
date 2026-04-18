#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-focus-wolfs-uri.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpFocusWolfsUriPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);

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
var Pages = CdpFocusWolfsUriPatterns.PageLine().Matches(List)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["EditPathFragment"], StringComparison.OrdinalIgnoreCase));
if (HitUrl is null)
{
    var (FbIdx, FbUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Strings["Needle"], StringComparison.OrdinalIgnoreCase));
    if (FbUrl is null)
    {
        await Console.Out.WriteLineAsync("no Console tab — opening new one");
        _ = await RunCdp(Paths.Cdp, Paths.Repo, $"new_page \"{Strings["EditUrl"]}\"");
        await Task.Delay(7000);
    }
    else
    {
        HitIdx = FbIdx;
        _ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        _ = await RunCdp(Paths.Cdp, Paths.Repo, $"navigate_page --type url --url \"{Strings["EditUrl"]}\"");
        await Task.Delay(5000);
    }
}
else
{
    _ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    await Task.Delay(1500);
}

var List2 = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages2 = CdpFocusWolfsUriPatterns.PageLine().Matches(List2)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();
var (EditIdx, EditUrl) = Pages2.FirstOrDefault(P => P.Url.Contains(Strings["EditPathFragment"], StringComparison.OrdinalIgnoreCase));
if (EditUrl is null) { await Console.Error.WriteLineAsync("could not reach OAuth client edit page"); return 4; }
_ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {EditIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
await Task.Delay(1500);

var Js = $"() => {{ const target = '{Strings["TargetValue"]}'; const inputs = Array.from(document.querySelectorAll('input')); const match = inputs.find(i => (i.value || '').trim() === target); if (!match) {{ const all = inputs.map((i, idx) => ({{idx: idx, val: (i.value || '').trim().slice(0, 100)}})).filter(o => o.val); return JSON.stringify({{notFound: true, all: all}}, null, 2); }} match.scrollIntoView({{block: 'center'}}); match.focus(); const allFields = inputs.map((i, idx) => ({{idx: idx, val: (i.value || '').trim().slice(0, 80), focused: i === document.activeElement}})).filter(o => o.val || o.focused); return JSON.stringify({{focused: true, all: allFields}}, null, 2); }}";
var Escaped = Js.Replace("\"", "\\\"", StringComparison.Ordinal);
var Result = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"{Escaped}\"");
await Console.Out.WriteLineAsync(Result);
return 0;

namespace Scripts
{
    internal static partial class CdpFocusWolfsUriPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
