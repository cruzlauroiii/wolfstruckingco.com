#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Spec = await File.ReadAllTextAsync(SpecPath);

string Get(string Name, string Default = "")
{
    var Match = Regex.Match(Spec, @"const\s+string\s+" + Name + @"\s*=\s*@?""(?<v>[^""]*)""\s*;");
    return Match.Success ? Match.Groups["v"].Value : Default;
}

var OutputPath = Get("OutputPath");
var Repos = Get("Repos")
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

using var Http = new HttpClient();
Http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("wolfstruckingco-ocr-research", "1.0"));
Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

var Rows = new List<(string Repo, string Name, int Stars, string Description, string Url)>();
foreach (var Repo in Repos)
{
    try
    {
        var Json = await Http.GetStringAsync("https://api.github.com/repos/" + Repo);
        using var Doc = JsonDocument.Parse(Json);
        var Root = Doc.RootElement;
        Rows.Add((
            Repo,
            Root.GetProperty("full_name").GetString() ?? Repo,
            Root.GetProperty("stargazers_count").GetInt32(),
            Root.TryGetProperty("description", out var Desc) ? Desc.GetString() ?? "" : "",
            Root.GetProperty("html_url").GetString() ?? ("https://github.com/" + Repo)
        ));
    }
    catch (Exception Ex)
    {
        Rows.Add((Repo, Repo, -1, "ERROR: " + Ex.Message, "https://github.com/" + Repo));
    }
}

Rows = Rows.OrderByDescending(Row => Row.Stars).ToList();
var Best = Rows.FirstOrDefault(Row => Row.Stars >= 0);
var Lines = new List<string>
{
    "# OCR GitHub Star Research",
    "",
    "This repo-local script checks candidate OCR engines through the GitHub API and ranks by stargazers.",
    "",
    "| Rank | Repository | Stars | URL |",
    "|---:|---|---:|---|"
};
var Rank = 1;
foreach (var Row in Rows)
{
    Lines.Add($"| {Rank++} | {Row.Name} | {Row.Stars} | {Row.Url} |");
}
Lines.Add("");
Lines.Add("## Selected Local OCR");
Lines.Add("");
Lines.Add(Best.Stars >= 0
    ? $"Selected: `{Best.Name}` because it has the highest GitHub stars among the configured local OCR candidates."
    : "Selected: none, because every GitHub API lookup failed.");
Lines.Add("");
Lines.Add("Note: the scene generator invokes local Python `PaddleOCR`. If the package is missing, it installs `paddleocr` in the foreground and retries once.");

var Output = string.Join(Environment.NewLine, Lines);
if (!string.IsNullOrEmpty(OutputPath))
{
    var Dir = Path.GetDirectoryName(OutputPath);
    if (!string.IsNullOrEmpty(Dir)) Directory.CreateDirectory(Dir);
    await File.WriteAllTextAsync(OutputPath, Output);
}
Console.WriteLine(Output);
return 0;
