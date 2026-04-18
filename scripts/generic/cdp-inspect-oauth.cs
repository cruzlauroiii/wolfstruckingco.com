#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-inspect-oauth.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpInspectOauthPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);

var UrlKeyRe = CdpInspectOauthPatterns.UrlMatchKey();
var Matches = Strings.Where(Kv => UrlKeyRe.IsMatch(Kv.Key)).Select(Kv => Kv.Value).ToList();
var ExpectedBase = Strings.TryGetValue("ExpectedCallbackBase", out var Eb) ? Eb : string.Empty;
var WorkerSubstring = Strings.TryGetValue("WorkerCallbackSubstring", out var Ws) ? Ws : string.Empty;
var WorkerRegex = Strings.TryGetValue("WorkerCallbackJsRegex", out var Wr) ? Wr : string.Empty;

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

var PageList = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
await Console.Out.WriteLineAsync("# Open pages");
await Console.Out.WriteLineAsync(PageList);
await Console.Out.WriteLineAsync();

var PageRe = CdpInspectOauthPatterns.PageLine();
var PageEntries = PageRe.Matches(PageList)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();

foreach (var Needle in Matches)
{
    var (HitIdx, HitUrl) = PageEntries.FirstOrDefault(P => P.Url.Contains(Needle, StringComparison.OrdinalIgnoreCase));
    if (HitUrl is null) { await Console.Out.WriteLineAsync($"## {Needle}: NO TAB OPEN"); await Console.Out.WriteLineAsync(); continue; }
    await Console.Out.WriteLineAsync($"## {Needle} (page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}) — {HitUrl}");
    _ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    var Scrape = await RunCdp(Paths.Cdp, Paths.Repo, $"evaluate_script \"() => {{ const sub = '{WorkerSubstring}'; const re = new RegExp('{WorkerRegex}', 'g'); const t = document.body && document.body.innerText || ''; const has = t.includes(sub); const inputs = Array.from(document.querySelectorAll('input,textarea')).map(i => (i.value||'').trim()).filter(v => v.length).slice(0,30); const callbacks = (t.match(re) || []); return JSON.stringify({{hasWorker: has, callbacks: callbacks, sampleInputs: inputs}}, null, 2); }}\"");
    await Console.Out.WriteLineAsync(Scrape);
    await Console.Out.WriteLineAsync();
}

await Console.Out.WriteLineAsync($"Expected callback base: {ExpectedBase}");
return 0;

namespace Scripts
{
    internal static partial class CdpInspectOauthPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^Url\d+Match$", RegexOptions.ExplicitCapture)]
        internal static partial Regex UrlMatchKey();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
