#:property TargetFramework=net11.0-windows10.0.19041.0
#:property UseWinRT=true

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

var repo = @"C:\repo\public\wolfstruckingco.com\main";
var frames = Path.Combine(Path.GetTempPath(), "wolfs-video", "frames");
var scenesPath = Path.Combine(repo, "docs", "videos", "scenes-final-v2.json");
var outPath = Path.Combine(repo, "docs", "videos", "ocr-narration-check.md");

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--repo" && i + 1 < args.Length) repo = args[++i];
    else if (args[i] == "--frames" && i + 1 < args.Length) frames = args[++i];
    else if (args[i] == "--scenes" && i + 1 < args.Length) scenesPath = args[++i];
    else if (args[i] == "--out" && i + 1 < args.Length) outPath = args[++i];
}

if (!File.Exists(scenesPath))
{
    await Console.Error.WriteLineAsync("Scenes file missing: " + scenesPath);
    return 1;
}

var scenes = JsonDocument.Parse(await File.ReadAllTextAsync(scenesPath)).RootElement.EnumerateArray().ToArray();
var engine = OcrEngine.TryCreateFromLanguage(new Language("en-US")) ?? OcrEngine.TryCreateFromUserProfileLanguages();
if (engine is null)
{
    await Console.Error.WriteLineAsync("Windows OCR engine is unavailable for en-US.");
    return 2;
}

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
var md = new StringBuilder();
md.AppendLine("# OCR vs Narration Check");
md.AppendLine();
md.AppendLine("Local frame text extracted with Windows OCR. Score is content-word overlap between narration and extracted frame text. Visual cue checks keep SSO, map, upload, and dashboard scenes from being marked bad only because the page does not repeat the narration word-for-word.");
md.AppendLine();
md.AppendLine("| # | Page | Frame | Score | Visual cue | Verdict | Narration | Extracted text |");
md.AppendLine("|---|---|---|---:|---|---|---|---|");

var weak = 0;
var staleTechnical = 0;
for (var idx = 0; idx < scenes.Length; idx++)
{
    var n = idx + 1;
    var pad = n.ToString("000", CultureInfo.InvariantCulture);
    var png = Path.Combine(frames, pad + ".png");
    var txt = Path.Combine(frames, pad + ".txt");
    var scene = scenes[idx];
    var narration = scene.TryGetProperty("narration", out var np) ? np.GetString() ?? "" : "";
    var target = scene.TryGetProperty("target", out var tp) ? tp.GetString() ?? "" : "";
    var extracted = File.Exists(txt) ? await File.ReadAllTextAsync(txt) : "";
    if (string.IsNullOrWhiteSpace(extracted) && File.Exists(png))
    {
        extracted = await OcrPng(engine, png);
    }

    var score = Overlap(narration, extracted);
    var (page, cue, cueMatched) = VisualCue(target, narration, extracted);
    if (HasStaleTechnicalText(extracted)) staleTechnical++;
    var verdict = score >= 0.45 ? "best" : score >= 0.25 ? "ok" : cueMatched ? "visual-ok" : "weak";
    if (verdict == "weak") weak++;
    var frameCell = File.Exists(png) ? $"![{pad}]({png.Replace("\\", "/")})" : "missing";
    md.Append("| ")
        .Append(pad).Append(" | ")
        .Append(Escape(page)).Append(" | ")
        .Append(frameCell).Append(" | ")
        .Append(score.ToString("P0", CultureInfo.InvariantCulture)).Append(" | ")
        .Append(cueMatched ? "matched: " : "missing: ").Append(Escape(cue)).Append(" | ")
        .Append(verdict).Append(" | ")
        .Append(Escape(narration)).Append(" | ")
        .Append(Escape(Clip(extracted, 240))).AppendLine(" |");
}

md.AppendLine();
md.AppendLine("Weak matches: **" + weak.ToString(CultureInfo.InvariantCulture) + "/" + scenes.Length.ToString(CultureInfo.InvariantCulture) + "**.");
md.AppendLine("Cached frames with old technical chat text: **" + staleTechnical.ToString(CultureInfo.InvariantCulture) + "/" + scenes.Length.ToString(CultureInfo.InvariantCulture) + "**.");
await File.WriteAllTextAsync(outPath, md.ToString());
Console.WriteLine(outPath);
return weak == 0 ? 0 : 3;

static async Task<string> OcrPng(OcrEngine engine, string path)
{
    var file = await StorageFile.GetFileFromPathAsync(path);
    await using var stream = await file.OpenStreamForReadAsync();
    var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
    using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    var result = await engine.RecognizeAsync(bitmap);
    return result.Text ?? "";
}

static double Overlap(string a, string b)
{
    var aw = Words(a).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var bw = Words(b).ToHashSet(StringComparer.OrdinalIgnoreCase);
    if (aw.Count == 0) return 0;
    return aw.Count(w => bw.Contains(w)) / (double)aw.Count;
}

static (string Page, string Cue, bool Matched) VisualCue(string target, string narration, string extracted)
{
    var page = PageName(target);
    var cue = page switch
    {
        "Login" => ProviderCue(narration),
        "Chat" => "chat agent",
        "Documents" => "documents upload",
        "Apply" => "apply drive",
        "Marketplace" => "marketplace listing",
        "Map" => "eta distance map",
        "Track" => "track delivered ship",
        "Dashboard" => "dashboard driver",
        "Admin" => "admin applicants",
        "HiringHall" => "hiring applicants",
        "Schedule" => "schedule route",
        "Dispatcher" => "dispatcher eta",
        "Investors/KPI" => "kpi revenue",
        var p when p.StartsWith("Buy/", StringComparison.Ordinal) => "buyer delivery pay notes",
        "" => "home wolfs trucking",
        _ => page.ToLowerInvariant(),
    };
    return (string.IsNullOrEmpty(page) ? "Home" : page, cue, HasAny(extracted, cue.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
}

static string PageName(string target)
{
    if (!Uri.TryCreate(target, UriKind.Absolute, out var uri)) return "";
    var path = uri.AbsolutePath.Trim('/');
    if (path.StartsWith("wolfstruckingco.com/", StringComparison.OrdinalIgnoreCase))
    {
        path = path["wolfstruckingco.com/".Length..];
    }
    else if (path.Equals("wolfstruckingco.com", StringComparison.OrdinalIgnoreCase))
    {
        path = "";
    }
    return path.Trim('/');
}

static string ProviderCue(string narration)
{
    var lower = narration.ToLowerInvariant();
    if (lower.Contains("google", StringComparison.Ordinal)) return "google sign";
    if (lower.Contains("microsoft", StringComparison.Ordinal)) return "microsoft sign";
    if (lower.Contains("github", StringComparison.Ordinal)) return "github sign";
    if (lower.Contains("okta", StringComparison.Ordinal)) return "okta sign";
    return "sign in";
}

static bool HasAny(string haystack, IEnumerable<string> needles)
{
    var lower = haystack.ToLowerInvariant();
    return needles.Any(n => lower.Contains(n.ToLowerInvariant(), StringComparison.Ordinal));
}

static bool HasStaleTechnicalText(string extracted)
{
    var lower = extracted.ToLowerInvariant();
    return lower.Contains("r2", StringComparison.Ordinal) ||
        lower.Contains("db_get", StringComparison.Ordinal) ||
        lower.Contains("db_put", StringComparison.Ordinal) ||
        lower.Contains("update r2", StringComparison.Ordinal);
}

static IEnumerable<string> Words(string s)
{
    foreach (Match m in Regex.Matches(s.ToLowerInvariant(), "[a-z0-9]{3,}"))
    {
        var w = m.Value;
        if (w is "the" or "and" or "with" or "for" or "that" or "this" or "from" or "your" or "you" or "are") continue;
        yield return w;
    }
}

static string Clip(string s, int len)
{
    s = Regex.Replace(s ?? "", "\\s+", " ").Trim();
    return s.Length <= len ? s : s[..len] + "...";
}

static string Escape(string s) => (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
