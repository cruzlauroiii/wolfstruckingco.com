#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
using System.Text;
using System.Text.Json;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != '\u0022') continue;
        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var ScenesJsonPath = Get("ScenesJsonPath")!;
var OcrDir = Get("OcrDir")!;
var OutputMd = Get("OutputMd")!;

var Json = await File.ReadAllTextAsync(ScenesJsonPath);
using var Doc = JsonDocument.Parse(Json);
var Scenes = Doc.RootElement.EnumerateArray().ToList();

static string Pad(string Url)
{
    var I = Url.IndexOf("cb=", StringComparison.Ordinal);
    if (I < 0) return string.Empty;
    var Start = I + 3;
    var Stop = Start;
    while (Stop < Url.Length && (char.IsDigit(Url[Stop]) || (Url[Stop] >= 'a' && Url[Stop] <= 'z'))) Stop++;
    return Url[Start..Stop];
}

static string PageSlug(string Url)
{
    var After = Url.IndexOf(".com/", StringComparison.Ordinal);
    if (After < 0) return string.Empty;
    var Tail = Url[(After + 5)..];
    var Q = Tail.IndexOf('?', StringComparison.Ordinal);
    if (Q >= 0) Tail = Tail[..Q];
    return Tail.TrimEnd('/');
}

static string[] Keywords(string Slug)
{
    var First = Slug.Split('/')[0];
    return First switch
    {
        "" => new[] { "Wolfs Trucking", "Move anything", "Apply to drive", "agent what" },
        "Login" => new[] { "Sign in", "Sign In", "Google", "Microsoft", "GitHub", "Okta" },
        "Chat" => new[] { "Chat with Agent", "sign in to chat", "Tell the agent", "What are you shipping" },
        "Marketplace" => new[] { "Marketplace", "No items for sale", "BYD", "Tesla", "Harley" },
        "Apply" => new[] { "Apply", "applicants", "hired", "pending" },
        "Documents" => new[] { "Documents", "Upload", "CDL", "badge" },
        "Dashboard" => new[] { "Dashboard", "Welcome", "QUICK LINKS", "JOB OFFER", "earnings" },
        "Map" => new[] { "Map", "ETA", "DISTANCE", "SPEED", "Take the exit", "Turn" },
        "Admin" => new[] { "Admin", "applicants", "pending" },
        "HiringHall" => new[] { "Hiring Hall", "applicants", "Approve all", "badges" },
        "Track" => new[] { "Track", "delivery", "Awaiting" },
        "Settings" => new[] { "Settings" },
        "Itinerary" => new[] { "Itinerary", "trip", "history" },
        "Buy" => new[] { "Step", "pay", "Receipt", "deliver", "address" },
        "Sell" => new[] { "Step", "pickup", "vehicle", "shipping" },
        "Investors" => new[] { "KPI", "Revenue", "Net Revenue", "Drivers Active", "Users" },
        "Job" => new[] { "Job", "Offer", "Accept" },
        "Schedule" => new[] { "Schedule", "hours", "week" },
        "Interview" => new[] { "Interview", "agent" },
        "Voice" => new[] { "Voice", "speak", "listen" },
        "Reviews" => new[] { "Reviews", "rating" },
        "FAQ" => new[] { "FAQ", "question" },
        "Pricing" => new[] { "Pricing", "price", "cost", "fee" },
        "Services" => new[] { "Services", "drayage", "OTR" },
        "Industries" => new[] { "Industries" },
        "Careers" => new[] { "Careers", "hiring", "apply" },
        "CareerAgent" => new[] { "Career", "agent" },
        "Contact" => new[] { "Contact", "email", "phone" },
        "About" => new[] { "About", "mission", "team" },
        "Blog" => new[] { "Blog", "article", "post" },
        "Privacy" => new[] { "Privacy" },
        "Terms" => new[] { "Terms" },
        "Sitemap" => new[] { "Sitemap" },
        "Case-Studies" => new[] { "Case" },
        "VoiceChat" => new[] { "Voice", "call", "Chat" },
        "Dispatcher" => new[] { "Dispatcher", "dispatch" },
        "Employer" => new[] { "Post a load", "Employer" },
        _ => new[] { First }
    };
}

static bool Pass(string Url, string Ocr)
{
    var Slug = PageSlug(Url);
    foreach (var K in Keywords(Slug))
    {
        if (Ocr.Contains(K, StringComparison.OrdinalIgnoreCase)) return true;
    }
    return false;
}

static int Similarity(string Narration, string OcrText)
{
    static IEnumerable<string> Words(string T)
    {
        var Sb = new StringBuilder();
        foreach (var Ch in T)
        {
            if (char.IsLetterOrDigit(Ch)) Sb.Append(char.ToLowerInvariant(Ch));
            else Sb.Append(' ');
        }
        return Sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
    var Nw = new HashSet<string>(Words(Narration));
    if (Nw.Count == 0) return 0;
    var Ow = new HashSet<string>(Words(OcrText));
    var Total = Nw.Count;
    Nw.IntersectWith(Ow);
    return (int)Math.Round(100.0 * Nw.Count / Math.Max(1, Total));
}

var Md = new StringBuilder();
Md.AppendLine("| Scene | URL | Narration | OCR Text | Sim % | Pass/Fail |");
Md.AppendLine("|-------|-----|-----------|----------|-------|-----------|");
var PassCount = 0;
var FailCount = 0;
foreach (var Scene in Scenes)
{
    var Url = Scene.GetProperty("target").GetString() ?? string.Empty;
    var Narration = Scene.GetProperty("narration").GetString() ?? string.Empty;
    var P = Pad(Url);
    if (string.IsNullOrEmpty(P)) continue;
    var TxtPath = Path.Combine(OcrDir, "scene-" + P + ".txt");
    var Ocr = File.Exists(TxtPath) ? await File.ReadAllTextAsync(TxtPath) : string.Empty;
    var OcrFlat = Ocr.Replace('|', '/').Replace('\n', ' ').Replace('\r', ' ').Trim();
    var Verdict = Pass(Url, OcrFlat) ? "pass" : "fail";
    if (Verdict == "pass") PassCount++; else FailCount++;
    if (OcrFlat.Length > 200) OcrFlat = OcrFlat[..200] + "...";
    var Sim = Similarity(Narration, Ocr);
    var EscNar = Narration.Replace("|", "/", StringComparison.Ordinal);
    Md.Append("| ").Append(P).Append(" | ").Append(Url).Append(" | ").Append(EscNar).Append(" | ").Append(OcrFlat).Append(" | ").Append(Sim.ToString(CultureInfo.InvariantCulture)).Append(" | ").Append(Verdict).AppendLine(" |");
}
Md.AppendLine();
Md.Append("Totals: pass=").Append(PassCount.ToString(CultureInfo.InvariantCulture)).Append(" fail=").AppendLine(FailCount.ToString(CultureInfo.InvariantCulture));
await File.WriteAllTextAsync(OutputMd, Md.ToString());
await Console.Error.WriteLineAsync("narration-vs-ocr: pass=" + PassCount.ToString(CultureInfo.InvariantCulture) + " fail=" + FailCount.ToString(CultureInfo.InvariantCulture));
return 0;
