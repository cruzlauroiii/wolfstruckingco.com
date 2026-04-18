#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-focus-oauth-fields.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strings = CdpFocusPatterns.ConstString().Matches(Body)
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

var PageList = await RunCdp(Paths.Cdp, Paths.Repo, "list_pages");
var Pages = CdpFocusPatterns.PageLine().Matches(PageList)
    .Select(M => (Idx: int.Parse(M.Groups["idx"].Value, System.Globalization.CultureInfo.InvariantCulture), Url: M.Groups["url"].Value))
    .ToList();

int? Find(string Needle)
{
    var (Idx, Url) = Pages.FirstOrDefault(P => P.Url.Contains(Needle, StringComparison.OrdinalIgnoreCase));
    return Url is null ? null : Idx;
}

for (var I = 1; I <= 4; I++)
{
    var Suffix = I.ToString(System.Globalization.CultureInfo.InvariantCulture);
    var LabelKey = $"P{Suffix}Label";
    var NeedleKey = $"P{Suffix}Needle";
    var CallbackKey = $"P{Suffix}Callback";
    var HintKey = $"P{Suffix}Hint";
    var NavUrlKey = $"P{Suffix}NavUrl";

    if (!Strings.TryGetValue(LabelKey, out var Label)) { continue; }
    var NeedleVal = Strings[NeedleKey];
    var Callback = Strings[CallbackKey];
    var Hint = Strings[HintKey];
    var NavUrl = Strings.TryGetValue(NavUrlKey, out var Nav) ? Nav : string.Empty;

    var Idx = Find(NeedleVal);
    if (Idx is int N)
    {
        var NStr = N.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(NavUrl))
        {
            _ = await RunCdp(Paths.Cdp, Paths.Repo, $"select_page {NStr}");
            _ = await RunCdp(Paths.Cdp, Paths.Repo, $"navigate_page --type url --url \"{NavUrl}\"");
            await Console.Out.WriteLineAsync($"{Label}: {Hint}:");
            await Console.Out.WriteLineAsync($"  {Callback}");
        }
        else
        {
            await Console.Out.WriteLineAsync($"{Label}: page {NStr} - {Hint}:");
            await Console.Out.WriteLineAsync($"  {Callback}");
        }
    }
    else
    {
        await Console.Out.WriteLineAsync($"{Label}: NO TAB");
    }
}

return 0;

namespace Scripts
{
    internal static partial class CdpFocusPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"^(?<idx>\d+):\s+(?<url>\S+)", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
        internal static partial Regex PageLine();
    }
}
