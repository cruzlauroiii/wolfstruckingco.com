#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// lh-issues.cs — read every Lighthouse JSON report under docs/videos/lighthouse-reports/
// and print the failing accessibility audits with their description.
//
//   dotnet run scripts/lh-issues.cs

using System.Text.Json;

var Repo = args.FirstOrDefault(A => Directory.Exists(A))
    ?? @"C:\repo\public\wolfstruckingco.com\main";
var Dir = Path.Combine(Repo, "docs", "videos", "lighthouse-reports");
if (!Directory.Exists(Dir))
{
    Console.Error.WriteLine($"missing: {Dir}");
    return 1;
}

foreach (var F in Directory.EnumerateFiles(Dir, "*.json"))
{
    var Doc = JsonDocument.Parse(File.ReadAllText(F));
    var Audits = Doc.RootElement.GetProperty("audits");
    var Failed = new List<(string Id, string Title)>();
    foreach (var Audit in Audits.EnumerateObject())
    {
        if (!Audit.Value.TryGetProperty("score", out var Score) || Score.ValueKind == JsonValueKind.Null)
        {
            continue;
        }
        if (Score.GetDouble() >= 1.0)
        {
            continue;
        }
        if (!Audit.Value.TryGetProperty("scoreDisplayMode", out var Mode) || Mode.GetString() == "manual" || Mode.GetString() == "notApplicable")
        {
            continue;
        }
        var Title = Audit.Value.GetProperty("title").GetString() ?? "?";
        Failed.Add((Audit.Name, Title));
    }
    if (Failed.Count == 0)
    {
        continue;
    }
    Console.WriteLine($"━━ {Path.GetFileNameWithoutExtension(F)} — {Failed.Count} fail(s) ━━");
    foreach (var (Id, Title) in Failed)
    {
        Console.WriteLine($"  {Id}: {Title}");
        var Audit = Audits.GetProperty(Id);
        if (Audit.TryGetProperty("details", out var Details) && Details.TryGetProperty("items", out var Items) && Items.ValueKind == JsonValueKind.Array)
        {
            foreach (var Item in Items.EnumerateArray().Take(3))
            {
                if (Item.TryGetProperty("node", out var Node) && Node.TryGetProperty("snippet", out var Snip))
                {
                    var Snippet = Snip.GetString() ?? "";
                    Console.WriteLine($"      → {(Snippet.Length > 100 ? Snippet[..100] + "…" : Snippet)}");
                }
            }
        }
    }
    Console.WriteLine();
}
return 0;
