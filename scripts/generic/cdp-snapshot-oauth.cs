#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-snapshot-oauth.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpSnapshotPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
var Matches = CdpSnapshotPatterns.UrlMatchConst().Matches(Body)
    .Select(M => M.Groups["url"].Value)
    .ToList();
var FilterKeyRe = CdpSnapshotPatterns.FilterKey();
var Filters = Strings.Where(Kv => FilterKeyRe.IsMatch(Kv.Key)).Select(Kv => Kv.Value).ToList();

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
var PageRe = CdpSnapshotPatterns.PageLine();
var Pages = PageRe.Matches(PageList)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();

foreach (var Needle in Matches)
{
    var (HitIdx, HitUrl) = Pages.FirstOrDefault(P => P.Url.Contains(Needle, StringComparison.OrdinalIgnoreCase));
    if (HitUrl is null) { await Console.Out.WriteLineAsync($"## {Needle}: NO TAB"); continue; }
    await Console.Out.WriteLineAsync($"## {Needle} (page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
    await Console.Out.WriteLineAsync($"   url: {HitUrl}");
    _ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {HitIdx.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    var Snap = await RunCdp(Paths.Cdp, Paths.Repo, "take_snapshot");
    foreach (var L in Snap.Split('\n').Where(L => Filters.Any(F => L.Contains(F, StringComparison.OrdinalIgnoreCase))).Take(30)) { await Console.Out.WriteLineAsync($"   {L.Trim()}"); }
    await Console.Out.WriteLineAsync();
}
return 0;

namespace Scripts
{
    internal static partial class CdpSnapshotPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex("""const\s+string\s+Url\d+Match\s*=\s*"(?<url>[^"]+)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex UrlMatchConst();

        [GeneratedRegex(@"^Filter[A-Z]$", RegexOptions.ExplicitCapture)]
        internal static partial Regex FilterKey();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
