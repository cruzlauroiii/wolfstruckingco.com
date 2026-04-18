#:property TargetFramework=net11.0
using System.Text.Json;

var Repo = args.FirstOrDefault(Directory.Exists)
    ?? @"C:\repo\public\wolfstruckingco.com\main";
var Dir = Path.Combine(Repo, "docs", "videos", "lighthouse-reports");
if (!Directory.Exists(Dir)) { await Console.Error.WriteLineAsync($"missing: {Dir}"); return 1; }

foreach (var F in Directory.EnumerateFiles(Dir, "*.json"))
{
    var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(F));
    var Audits = Doc.RootElement.GetProperty("audits");
    var Failed = new List<(string Id, string Title)>();
    foreach (var Audit in Audits.EnumerateObject())
    {
        if (!Audit.Value.TryGetProperty("score", out var Score) || Score.ValueKind == JsonValueKind.Null) { continue; }
        if (Score.GetDouble() >= 1.0) { continue; }
        if (!Audit.Value.TryGetProperty("scoreDisplayMode", out var Mode) || Mode.GetString() == "manual" || Mode.GetString() == "notApplicable") { continue; }
        var Title = Audit.Value.GetProperty("title").GetString() ?? "?";
        Failed.Add((Audit.Name, Title));
    }
    if (Failed.Count == 0) { continue; }
    await Console.Out.WriteLineAsync($"{Path.GetFileNameWithoutExtension(F)}: {Failed.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)} fail(s)");
    foreach (var (Id, Title) in Failed)
    {
        await Console.Out.WriteLineAsync($"  {Id}: {Title}");
        var Audit = Audits.GetProperty(Id);
        if (Audit.TryGetProperty("details", out var Details) && Details.TryGetProperty("items", out var Items) && Items.ValueKind == JsonValueKind.Array)
        {
            foreach (var Item in Items.EnumerateArray().Take(3))
            {
                if (Item.TryGetProperty("node", out var Node) && Node.TryGetProperty("snippet", out var Snip))
                {
                    var Snippet = Snip.GetString() ?? string.Empty;
                    await Console.Out.WriteLineAsync($"    {(Snippet.Length > 100 ? Snippet[..100] + "..." : Snippet)}");
                }
            }
        }
    }
}
return 0;
