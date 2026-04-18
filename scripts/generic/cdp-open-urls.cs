#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/cdp-open-urls.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Urls = CdpOpenUrlsPatterns.UrlConst().Matches(Body)
    .Select(M => M.Groups["url"].Value)
    .ToList();
if (Urls.Count == 0) { await Console.Error.WriteLineAsync("no const string Url* declarations found"); return 3; }

foreach (var Url in Urls)
{
    var Psi = new ProcessStartInfo("dotnet", $"run \"{Paths.Cdp}\" -- new_page \"{Url}\"")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Paths.Repo,
    };
    using var Proc = Process.Start(Psi)!;
    await Proc.WaitForExitAsync();
    await Console.Out.WriteLineAsync($"opened: {Url} (exit {Proc.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
}
return 0;

namespace Scripts
{
    internal static partial class CdpOpenUrlsPatterns
    {
        [GeneratedRegex("""const\s+string\s+Url\d+\s*=\s*"(?<url>[^"]+)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex UrlConst();
    }
}
