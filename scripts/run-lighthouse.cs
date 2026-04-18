#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// run-lighthouse.cs — run Google Lighthouse against each docs/<Page>/index.html
// (served via the local HTTPS dev server on :8443) and report Performance,
// Accessibility, Best Practices, and SEO scores. Goal per user point 14: 100%.
//
//   dotnet run scripts/run-lighthouse.cs                                          # default repo + lighthouse path
//   dotnet run scripts/run-lighthouse.cs -- C:\…\main C:\repo\todelete\lighthouse  # explicit
//
// Requires: lighthouse repo at C:\repo\todelete\lighthouse (cli/index.js), node 22+,
// local server running at https://localhost:8443.

using System.Diagnostics;
using System.Text.Json;

var Repo = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

var DocsRoot = Path.Combine(Repo, "docs");
var Pages = new[] { "", "About", "Services", "Pricing", "Marketplace", "Login", "Dashboard", "HiringHall", "Settings" };
var BaseUrl = "https://localhost:8443/wolfstruckingco.com/";
var ReportDir = Path.Combine(Repo, "docs", "videos", "lighthouse-reports");
Directory.CreateDirectory(ReportDir);

Console.WriteLine($"running Lighthouse against {Pages.Length} page(s)…");
Console.WriteLine();
Console.WriteLine($"{"page",-15}  {"perf",-6}  {"a11y",-6}  {"bp",-6}  {"seo",-6}");

foreach (var Page in Pages)
{
    var Url = BaseUrl + (string.IsNullOrEmpty(Page) ? "" : Page + "/");
    var ReportPath = Path.Combine(ReportDir, (string.IsNullOrEmpty(Page) ? "index" : Page) + ".json");
    var Psi = new ProcessStartInfo("npx",
        $"--yes lighthouse@latest \"{Url}\" --output=json --output-path=\"{ReportPath}\" --quiet --chrome-flags=\"--headless --ignore-certificate-errors --no-sandbox\" --only-categories=performance,accessibility,best-practices,seo")
    {
        UseShellExecute = true,
        CreateNoWindow = true,
    };
    using var Proc = Process.Start(Psi)!;
    Proc.WaitForExit();
    if (!File.Exists(ReportPath))
    {
        Console.WriteLine($"{Page,-15}  ✗ no report (exit {Proc.ExitCode})");
        continue;
    }
    var Json = JsonDocument.Parse(File.ReadAllText(ReportPath));
    var Cats = Json.RootElement.GetProperty("categories");
    int Score(string Key) => (int)Math.Round(Cats.GetProperty(Key).GetProperty("score").GetDouble() * 100);
    Console.WriteLine($"{(string.IsNullOrEmpty(Page) ? "/" : Page),-15}  {Score("performance"),-6}  {Score("accessibility"),-6}  {Score("best-practices"),-6}  {Score("seo"),-6}");
}

Console.WriteLine();
Console.WriteLine($"reports → {Path.GetRelativePath(Repo, ReportDir)}");
return 0;
