#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
using System.Net.Http;
using System.Text;

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
var SettleSeconds = int.Parse(Get("SettleSeconds") ?? "10", CultureInfo.InvariantCulture);

static string EscJsonString(string S)
{
    var Sb = new StringBuilder();
    foreach (var Ch in S)
    {
        if (Ch == '\u0022') Sb.Append("\\\u0022");
        else if (Ch == '\\') Sb.Append("\\\\");
        else if (Ch == '\n') Sb.Append("\\n");
        else if (Ch == '\r') Sb.Append("\\r");
        else if (Ch == '\t') Sb.Append("\\t");
        else if (Ch < 0x20) Sb.Append("\\u").Append(((int)Ch).ToString("x4", CultureInfo.InvariantCulture));
        else Sb.Append(Ch);
    }
    return Sb.ToString();
}

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
await Console.Error.WriteLineAsync("probe: list_pages=" + UrlList.Trim());
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
await Console.Error.WriteLineAsync("probe: pageIdx=" + PageIdx);
var Js = "() => JSON.stringify({ url: location.href, title: document.title, bodyLen: document.body ? document.body.innerText.length : -1, bodyHead: document.body ? document.body.innerText.slice(0, 1500) : '' })";
var Cmd = "evaluate_script --pageId " + PageIdx + " --function \u0022" + EscJsonString(Js) + "\u0022";
var R2 = await Post(Cmd);
var Body = R2.Trim();
await Console.Error.WriteLineAsync("probe: url=" + Url);
await Console.Error.WriteLineAsync("probe: body_len=" + Body.Length.ToString(CultureInfo.InvariantCulture));
if (!string.IsNullOrEmpty(Keywords))
{
    var Parts = Keywords.Split(',');
    foreach (var K in Parts)
    {
        var Kt = K.Trim();
        if (Kt.Length == 0) continue;
        var Hit = Body.Contains(Kt, StringComparison.OrdinalIgnoreCase);
        await Console.Error.WriteLineAsync("probe: kw='" + Kt + "' " + (Hit ? "HIT" : "miss"));
    }
}
await Console.Error.WriteLineAsync("probe: head=" + (Body.Length > 600 ? Body[..600] : Body));
return 0;
