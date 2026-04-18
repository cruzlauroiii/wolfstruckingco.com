#:property TargetFramework=net11.0
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var Strs = StripInlinePatterns.ConstString().Matches(await File.ReadAllTextAsync(SpecPath))
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
if (!Strs.TryGetValue("DocsRoot", out var DocsRoot)) { return 3; }
if (!Strs.TryGetValue("CssSource", out var CssSource)) { return 3; }
if (!Strs.TryGetValue("CssLinkHref", out var CssLinkHref)) { return 3; }
if (!Directory.Exists(DocsRoot)) { return 4; }

var DocsCssDir = Path.Combine(DocsRoot, "css");
Directory.CreateDirectory(DocsCssDir);
if (File.Exists(CssSource)) { File.Copy(CssSource, Path.Combine(DocsCssDir, "app.css"), true); }

var StyleRe = StripInlinePatterns.StyleBlock();
var InlineScriptRe = StripInlinePatterns.InlineScriptBlock();
var LinkTag = $"<link rel=\"stylesheet\" href=\"{CssLinkHref}\">";

var Touched = 0;
foreach (var F in Directory.EnumerateFiles(DocsRoot, "*.html", SearchOption.AllDirectories))
{
    var Body = await File.ReadAllTextAsync(F);
    var Original = Body;
    Body = StyleRe.Replace(Body, LinkTag);
    Body = InlineScriptRe.Replace(Body, string.Empty);
    if (Body != Original)
    {
        await File.WriteAllTextAsync(F, Body);
        Touched++;
    }
}
return 0;

namespace Scripts
{
    internal static partial class StripInlinePatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"<style>[\s\S]*?</style>", RegexOptions.IgnoreCase)]
        internal static partial Regex StyleBlock();

        [GeneratedRegex(@"<script(?![^>]*\bsrc=)[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase)]
        internal static partial Regex InlineScriptBlock();
    }
}
