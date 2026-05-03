#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

string? ReadStr(string Name)
{
    foreach (var Line in File.ReadAllLines(SpecPath))
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        if (After.StartsWith("@", StringComparison.Ordinal)) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}
int ReadInt(string Name, int Default)
{
    foreach (var Line in File.ReadAllLines(SpecPath))
    {
        var Idx = Line.IndexOf("const int " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 10 + Name.Length + 3);
        var Semi = After.IndexOf(";", StringComparison.Ordinal);
        if (Semi < 0) continue;
        if (int.TryParse(After.Substring(0, Semi), out var V)) return V;
    }
    return Default;
}

var Url = ReadStr("Url");
var OutputPath = ReadStr("OutputPath");
var HydrateMs = ReadInt("HydrateMs", 6000);
var DebugPort = ReadInt("DebugPort", 9222);
if (Url is null || OutputPath is null) return 3;

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
var TargetsJson = await Http.GetStringAsync($"http://127.0.0.1:{DebugPort}/json/new");
var Target = JsonNode.Parse(TargetsJson)!;
var WsUrl = Target["webSocketDebuggerUrl"]!.GetValue<string>();
var TargetId = Target["id"]!.GetValue<string>();

using var Ws = new ClientWebSocket();
Ws.Options.SetRequestHeader("User-Agent", "WolfsCapture/1.0");
await Ws.ConnectAsync(new Uri(WsUrl), CancellationToken.None);

var MsgId = 0;
var PendingResponses = new Dictionary<int, TaskCompletionSource<JsonNode>>();
var ReceiveCts = new CancellationTokenSource();
var ReceiveTask = Task.Run(async () =>
{
    var Buf = new byte[1 << 20];
    var Sb = new StringBuilder();
    while (Ws.State == WebSocketState.Open && !ReceiveCts.IsCancellationRequested)
    {
        Sb.Clear();
        WebSocketReceiveResult Result;
        do
        {
            try { Result = await Ws.ReceiveAsync(new ArraySegment<byte>(Buf), ReceiveCts.Token); }
            catch { return; }
            Sb.Append(Encoding.UTF8.GetString(Buf, 0, Result.Count));
        } while (!Result.EndOfMessage && Ws.State == WebSocketState.Open);
        var Json = JsonNode.Parse(Sb.ToString());
        if (Json is null) continue;
        if (Json["id"] is JsonNode IdNode)
        {
            var Id = IdNode.GetValue<int>();
            if (PendingResponses.TryGetValue(Id, out var Tcs)) { PendingResponses.Remove(Id); Tcs.TrySetResult(Json); }
        }
    }
});

async Task<JsonNode> Send(string Method, JsonObject? Params = null)
{
    var Id = ++MsgId;
    var Msg = new JsonObject { ["id"] = Id, ["method"] = Method };
    if (Params is not null) Msg["params"] = Params;
    var Tcs = new TaskCompletionSource<JsonNode>();
    PendingResponses[Id] = Tcs;
    var Bytes = Encoding.UTF8.GetBytes(Msg.ToJsonString());
    await Ws.SendAsync(new ArraySegment<byte>(Bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    var Done = await Task.WhenAny(Tcs.Task, Task.Delay(20000));
    if (Done != Tcs.Task) { PendingResponses.Remove(Id); throw new TimeoutException($"{Method} timed out"); }
    return await Tcs.Task;
}

try
{
    await Send("Page.enable");
    await Send("Network.enable");
    await Send("Network.clearBrowserCache");
    await Send("Page.navigate", new JsonObject { ["url"] = Url });
    await Task.Delay(HydrateMs);
    var ScreenshotResp = await Send("Page.captureScreenshot", new JsonObject { ["format"] = "png", ["captureBeyondViewport"] = false });
    var DataB64 = ScreenshotResp["result"]?["data"]?.GetValue<string>();
    if (string.IsNullOrEmpty(DataB64)) { await Console.Error.WriteLineAsync("no screenshot data"); return 4; }
    var OutDir = Path.GetDirectoryName(OutputPath);
    if (!string.IsNullOrEmpty(OutDir) && !Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);
    File.WriteAllBytes(OutputPath, Convert.FromBase64String(DataB64));
    await Console.Error.WriteLineAsync($"captured {Url} -> {OutputPath} ({new FileInfo(OutputPath).Length:N0} bytes)");
}
finally
{
    try { await Http.GetStringAsync($"http://127.0.0.1:{DebugPort}/json/close/{TargetId}"); } catch { }
    ReceiveCts.Cancel();
    if (Ws.State == WebSocketState.Open) { try { await Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None); } catch { } }
}
return 0;
