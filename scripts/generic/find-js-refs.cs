#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Text.RegularExpressions;
using Scripts;
string[] HtmlRoots = ["docs", "wwwroot", "src"];
var Refs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var ScriptRe = JsRefPatterns.ScriptSrc();

foreach (var R in HtmlRoots)
{
    var Full = Path.Combine(Paths.Repo, R);
    if (!Directory.Exists(Full)) { continue; }
    foreach (var F in Directory.EnumerateFiles(Full, "*.*", SearchOption.AllDirectories))
    {
        var Ext = Path.GetExtension(F).ToLowerInvariant();
        if (Ext is not (".html" or ".razor" or ".cshtml")) { continue; }
        if (F.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase)) { continue; }
        if (F.Contains(@"\obj\", StringComparison.OrdinalIgnoreCase)) { continue; }
        string Body;
        try { Body = await File.ReadAllTextAsync(F); } catch (IOException) { continue; }
        foreach (var Path2 in ScriptRe.Matches(Body).Select(M => M.Groups["src"].Value))
        {
            var BaseName = Path.GetFileName(Path2);
            Refs.TryGetValue(BaseName, out var N);
            Refs[BaseName] = N + 1;
        }
    }
}

foreach (var Kv in Refs.OrderByDescending(K => K.Value))
{
    await Console.Out.WriteLineAsync($"{Kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),4}  {Kv.Key}");
}
return 0;

namespace Scripts
{
    internal static partial class JsRefPatterns
    {
        [GeneratedRegex(@"<script[^>]+src\s*=\s*[""'](?<src>[^""']+\.js)[""']", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
        internal static partial Regex ScriptSrc();
    }
}
