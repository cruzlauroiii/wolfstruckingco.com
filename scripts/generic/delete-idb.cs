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

var BaseUrl = Get("BaseUrl") ?? "https://cruzlauroiii.github.io/wolfstruckingco.com/";
var ServeUrl = Get("ServeUrl") ?? "http://127.0.0.1:9334/";
var DbName = Get("DbName") ?? "wolfs";
var SettleSeconds = int.Parse(Get("SettleSeconds") ?? "6", CultureInfo.InvariantCulture);

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

await Post("new_page --url " + BaseUrl);
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

var Js = "() => { return new Promise((res) => { try { localStorage.clear(); sessionStorage.clear(); } catch(e) {} const req = indexedDB.deleteDatabase('" + DbName + "'); req.onsuccess = () => res('idb-deleted'); req.onerror = () => res('idb-error:' + (req.error && req.error.message)); req.onblocked = () => res('idb-blocked'); }); }";
var Cmd = "evaluate_script --pageId " + PageIdx + " --function \u0022" + EscJsonString(Js) + "\u0022";
var R = await Post(Cmd);
await Console.Error.WriteLineAsync("delete-idb: " + R.Trim());
return 0;
