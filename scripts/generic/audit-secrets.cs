#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

(string Name, Regex Re)[] Patterns =
[
    ("Anthropic API key", SecretPatterns.Anthropic()),
    ("OpenAI API key", SecretPatterns.OpenAI()),
    ("GitHub PAT classic", SecretPatterns.GithubPat()),
    ("GitHub PAT fine-grained", SecretPatterns.GithubPatFine()),
    ("GitHub OAuth", SecretPatterns.GithubOauth()),
    ("AWS access key", SecretPatterns.AwsAccessKey()),
    ("Google API key", SecretPatterns.GoogleApiKey()),
    ("Slack token", SecretPatterns.SlackToken()),
    ("Generic JWT", SecretPatterns.Jwt()),
];

var Tracked = new List<string>();
var Psi = new ProcessStartInfo("git", "ls-files") { RedirectStandardOutput = true, UseShellExecute = false, WorkingDirectory = Paths.Repo };
using (var P = Process.Start(Psi)!)
{
    string? Line;
    while ((Line = await P.StandardOutput.ReadLineAsync()) is not null) { Tracked.Add(Line); }
    await P.WaitForExitAsync();
}

var Findings = 0;
foreach (var F in Tracked)
{
    var Full = Path.Combine(Paths.Repo, F.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(Full)) { continue; }
    var Ext = Path.GetExtension(F).ToLowerInvariant();
    if (Ext is ".wasm" or ".dll" or ".pdb" or ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".mp4" or ".webm" or ".gz" or ".br") { continue; }
    string Body;
    try { Body = await File.ReadAllTextAsync(Full); }
    catch (IOException) { continue; }
    catch (UnauthorizedAccessException) { continue; }
    foreach (var (Name, Re) in Patterns)
    {
        foreach (Match M in Re.Matches(Body))
        {
            var Idx = M.Index;
            var Line = Body[..Idx].Count(C => C == '\n') + 1;
            var Snippet = M.Value.Length > 40 ? $"{M.Value[..20]}...{M.Value[^10..]}" : M.Value;
            if (Snippet.Contains("placeholder", StringComparison.OrdinalIgnoreCase) || Snippet.Contains("example", StringComparison.OrdinalIgnoreCase) || Snippet.Contains("xxxxx", StringComparison.OrdinalIgnoreCase)) { continue; }
            Console.WriteLine($"  {F}:{Line.ToString(System.Globalization.CultureInfo.InvariantCulture)} - {Name}: {Snippet}");
            Findings++;
        }
    }
}
return Findings > 0 ? 1 : 0;

namespace Scripts
{
    internal static partial class SecretPatterns
    {
        [GeneratedRegex(@"sk-ant-(?:api03|oat01)-[A-Za-z0-9_\-]{40,}")]
        internal static partial Regex Anthropic();

        [GeneratedRegex("sk-[A-Za-z0-9]{40,}")]
        internal static partial Regex OpenAI();

        [GeneratedRegex("ghp_[A-Za-z0-9]{36,}")]
        internal static partial Regex GithubPat();

        [GeneratedRegex("github_pat_[A-Za-z0-9_]{60,}")]
        internal static partial Regex GithubPatFine();

        [GeneratedRegex("gho_[A-Za-z0-9]{36,}")]
        internal static partial Regex GithubOauth();

        [GeneratedRegex("AKIA[0-9A-Z]{16}")]
        internal static partial Regex AwsAccessKey();

        [GeneratedRegex(@"AIza[0-9A-Za-z_\-]{35}")]
        internal static partial Regex GoogleApiKey();

        [GeneratedRegex(@"xox[abprs]-[A-Za-z0-9\-]{10,}")]
        internal static partial Regex SlackToken();

        [GeneratedRegex(@"eyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}")]
        internal static partial Regex Jwt();
    }
}
