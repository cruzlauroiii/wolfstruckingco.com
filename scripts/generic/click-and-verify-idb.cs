#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

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

var Url = Get("Url")!;
var ButtonPattern = Get("ButtonPattern")!;
var Store = Get("Store")!;
var ServeUrl = Get("ServeUrl") ?? "http://127.0.0.1:9334/";
var SettleSeconds = int.Parse(Get("SettleSeconds") ?? "15", CultureInfo.InvariantCulture);

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
async Task<string> Post(string Cmd)
{
    using var Req = new HttpRequestMessage(HttpMethod.Post, new Uri(ServeUrl)) { Content = new StringContent(Cmd) };
    using var R = await Http.SendAsync(Req).ConfigureAwait(false);
    return await R.Content.ReadAsStringAsync().ConfigureAwait(false);
}

string FindPageIdx(string Listing)
{
    foreach (var Ln in Listing.Split('\n'))
    {
        var T = Ln.Trim();
        if (!T.Contains("wolfstruckingco", StringComparison.OrdinalIgnoreCase)) continue;
        var Colon = T.IndexOf(':');
        if (Colon < 1) continue;
        var Idx = T[..Colon].Trim();
        if (Idx.All(char.IsDigit)) return Idx;
    }
    return "1";
}

async Task<int> StoreCount(string PageIdx)
{
    var CountJs = "() => new Promise((res) => { const r = indexedDB.open('wolfs', 2); r.onsuccess = () => { try { const tx = r.result.transaction('" + Store + "', 'readonly'); const req = tx.objectStore('" + Store + "').count(); req.onsuccess = () => res(req.result); req.onerror = () => res(-1); } catch(e) { res(-2); } }; r.onerror = () => res(-3); })";
    var Cmd = "evaluate_script --pageId " + PageIdx + " --function \u0022" + EscJsonString(CountJs) + "\u0022";
    var R = await Post(Cmd);
    var M = Regex.Match(R, @"\d+");
    return M.Success ? int.Parse(M.Value, CultureInfo.InvariantCulture) : -99;
}

await Post("new_page --url " + Url);
await Task.Delay(TimeSpan.FromSeconds(SettleSeconds));
var PageIdx = FindPageIdx(await Post("list_pages"));
await Console.Error.WriteLineAsync("click-and-verify: pageIdx=" + PageIdx + " url=" + Url);

var Before = await StoreCount(PageIdx);
await Console.Error.WriteLineAsync("click-and-verify: before count=" + Before.ToString(CultureInfo.InvariantCulture));

var Snap = await Post("take_snapshot --pageId " + PageIdx);
var Rx = new Regex(@"^\s*\[(\d+)\]\s+button\s+\u0022[^\u0022]*" + Regex.Escape(ButtonPattern), RegexOptions.Multiline | RegexOptions.IgnoreCase);
var M2 = Rx.Match(Snap);
if (!M2.Success)
{
    await Console.Error.WriteLineAsync("click-and-verify: button '" + ButtonPattern + "' not found in snapshot (" + Snap.Length.ToString(CultureInfo.InvariantCulture) + " chars)");
    return 2;
}
var Uid = M2.Groups[1].Value;
await Console.Error.WriteLineAsync("click-and-verify: button uid=" + Uid);

await Post("click --pageId " + PageIdx + " --uid " + Uid);
await Task.Delay(TimeSpan.FromSeconds(3));

var After = await StoreCount(PageIdx);
await Console.Error.WriteLineAsync("click-and-verify: after count=" + After.ToString(CultureInfo.InvariantCulture));
await Console.Error.WriteLineAsync("click-and-verify: delta=" + (After - Before).ToString(CultureInfo.InvariantCulture));
return (After - Before > 0) ? 0 : 3;
