#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include ../specific/run-lighthouse-config.cs
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Scripts;

var Repo = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

var Pages = typeof(RunLighthouseConfig)
    .GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(F => F.IsLiteral && F.Name.StartsWith("Page_", StringComparison.Ordinal))
    .OrderBy(F => F.Name, StringComparer.Ordinal)
    .Select(F => (string)F.GetRawConstantValue()!)
    .ToArray();
var BaseUrl = RunLighthouseConfig.BaseUrl;
var ReportDir = Path.Combine(Repo, RunLighthouseConfig.ReportSubdir.Replace('/', Path.DirectorySeparatorChar));
Directory.CreateDirectory(ReportDir);

foreach (var Page in Pages)
{
    var Url = BaseUrl + (string.IsNullOrEmpty(Page) ? string.Empty : $"{Page}/");
    var ReportPath = Path.Combine(ReportDir, $"{(string.IsNullOrEmpty(Page) ? "index" : Page)}.json");
    var Psi = new ProcessStartInfo(
        "npx",
        $"--yes lighthouse@latest \"{Url}\" --output=json --output-path=\"{ReportPath}\" --quiet --chrome-flags=\"--headless --ignore-certificate-errors --no-sandbox\" --only-categories=performance,accessibility,best-practices,seo")
    {
        UseShellExecute = true,
        CreateNoWindow = true,
    };
    using var Proc = Process.Start(Psi)!;
    await Proc.WaitForExitAsync();
    if (!File.Exists(ReportPath)) { await Console.Error.WriteLineAsync($"{Page}: no report"); continue; }
    var Json = JsonDocument.Parse(await File.ReadAllTextAsync(ReportPath));
    var Cats = Json.RootElement.GetProperty("categories");
    int Score(string Key) => (int)Math.Round(Cats.GetProperty(Key).GetProperty("score").GetDouble() * 100);
    await Console.Out.WriteLineAsync(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{(string.IsNullOrEmpty(Page) ? "/" : Page),-15}  {Score("performance"),-6}  {Score("accessibility"),-6}  {Score("best-practices"),-6}  {Score("seo"),-6}"));
}
return 0;

