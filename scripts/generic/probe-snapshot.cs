#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
using System.Net.Http;

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

var Url = Get("Url")!;
var Keywords = Get("Keywords") ?? "";
var ServeUrl = Get("ServeUrl") ?? "http://127.0.0.1:9334/";
var SettleSeconds = int.Parse(Get("SettleSeconds") ?? "15", CultureInfo.InvariantCulture);

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
async Task<string> Post(string Cmd)
{
    using var Req = new HttpRequestMessage(HttpMethod.Post, new Uri(ServeUrl)) { Content = new StringContent(Cmd) };
    using var R = await Http.SendAsync(Req).ConfigureAwait(false);
    return await R.Content.ReadAsStringAsync().ConfigureAwait(false);
}

await Post("new_page --url " + Url);
await Task.Delay(TimeSpan.FromSeconds(SettleSeconds));

var UrlList = await Post("list_pages");
var PageIdx = "1";
foreach (var Ln in UrlList.Split('\n'))
{
    var T = Ln.Trim();
    if (!T.Contains("wolfstruckingco", StringComparison.OrdinalIgnoreCase)) continue;
    var Colon = T.IndexOf(':');
    if (Colon < 1) continue;
    var Idx = T[..Colon].Trim();
    if (Idx.All(char.IsDigit)) { PageIdx = Idx; break; }
}
await Console.Error.WriteLineAsync("probe-snapshot: pageIdx=" + PageIdx);

var Snap = await Post("take_snapshot --pageId " + PageIdx);
await Console.Error.WriteLineAsync("probe-snapshot: snap_len=" + Snap.Length.ToString(CultureInfo.InvariantCulture));
if (!string.IsNullOrEmpty(Keywords))
{
    foreach (var K in Keywords.Split(','))
    {
        var Kt = K.Trim();
        if (Kt.Length == 0) continue;
        var Hit = Snap.Contains(Kt, StringComparison.OrdinalIgnoreCase);
        await Console.Error.WriteLineAsync("probe-snapshot: kw='" + Kt + "' " + (Hit ? "HIT" : "miss"));
    }
}
return 0;
