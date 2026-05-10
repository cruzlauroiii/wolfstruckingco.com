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
var ButtonPattern = Get("ButtonPattern")!;
var ServeUrl = Get("ServeUrl") ?? "http://127.0.0.1:9334/";
var SettleSeconds = int.Parse(Get("SettleSeconds") ?? "20", CultureInfo.InvariantCulture);

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

await Post("new_page --url " + Url);
await Task.Delay(TimeSpan.FromSeconds(SettleSeconds));
var PageIdx = FindPageIdx(await Post("list_pages"));

var Snap = await Post("take_snapshot --pageId " + PageIdx);
var Rx = new System.Text.RegularExpressions.Regex(@"^\s*\[(\d+)\]\s+button\s+\u0022[^\u0022]*" + System.Text.RegularExpressions.Regex.Escape(ButtonPattern), System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
var M = Rx.Match(Snap);
if (M.Success)
{
    await Console.Error.WriteLineAsync("read-console: clicking uid=" + M.Groups[1].Value);
    await Post("click --pageId " + PageIdx + " --uid " + M.Groups[1].Value);
    await Task.Delay(TimeSpan.FromSeconds(3));
}
else
{
    await Console.Error.WriteLineAsync("read-console: button '" + ButtonPattern + "' not found");
}

var Console2 = await Post("list_console_messages --pageId " + PageIdx);
await Console.Error.WriteLineAsync("read-console: console output:");
await Console.Error.WriteLineAsync(Console2.Length > 4000 ? Console2[..4000] : Console2);
return 0;
