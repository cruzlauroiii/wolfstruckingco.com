#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
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

var Port = int.Parse(Get("Port") ?? "4444", System.Globalization.CultureInfo.InvariantCulture);
var Listener = new HttpListener();
Listener.Prefixes.Add("http://127.0.0.1:" + Port.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/");
Listener.Prefixes.Add("http://localhost:" + Port.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/");
Listener.Start();
await Console.Out.WriteLineAsync("tunnel-server listening on :" + Port.ToString(System.Globalization.CultureInfo.InvariantCulture));

var Clients = new ConcurrentDictionary<string, WebSocket>();
var CmdSenders = new ConcurrentDictionary<string, WebSocket>();

static async Task SendJson(WebSocket Ws, string Body)
{
    var Bytes = Encoding.UTF8.GetBytes(Body);
    await Ws.SendAsync(Bytes, WebSocketMessageType.Text, true, CancellationToken.None);
}

static async Task<string> ReceiveJson(WebSocket Ws)
{
    var Sb = new StringBuilder();
    var Buf = new byte[1 << 16];
    WebSocketReceiveResult R;
    do
    {
        R = await Ws.ReceiveAsync(Buf, CancellationToken.None);
        if (R.MessageType == WebSocketMessageType.Close) return string.Empty;
        Sb.Append(Encoding.UTF8.GetString(Buf, 0, R.Count));
    } while (!R.EndOfMessage);
    return Sb.ToString();
}

async Task HandlePeer(WebSocket Ws)
{
    string? Name = null;
    try
    {
        while (Ws.State == WebSocketState.Open)
        {
            var Body = await ReceiveJson(Ws);
            if (string.IsNullOrEmpty(Body)) break;
            using var Doc = JsonDocument.Parse(Body);
            var Root = Doc.RootElement;
            var Type = Root.GetProperty("type").GetString() ?? string.Empty;
            if (Type == "subscribe")
            {
                Name = Root.GetProperty("client").GetString();
                if (!string.IsNullOrEmpty(Name)) Clients[Name] = Ws;
                await Console.Out.WriteLineAsync("subscribe: " + (Name ?? "<null>"));
            }
            else if (Type == "cmd")
            {
                var Id = Root.GetProperty("id").GetString() ?? string.Empty;
                var Target = Root.GetProperty("target").GetString() ?? string.Empty;
                CmdSenders[Id] = Ws;
                if (Clients.TryGetValue(Target, out var TargetWs))
                {
                    await SendJson(TargetWs, Body);
                    await Console.Out.WriteLineAsync("cmd " + Id + " -> " + Target);
                }
                else
                {
                    var Err = "{\u0022type\u0022:\u0022result\u0022,\u0022id\u0022:\u0022" + Id + "\u0022,\u0022client\u0022:\u0022server\u0022,\u0022exit_code\u0022:404,\u0022stdout\u0022:\u0022\u0022,\u0022stderr\u0022:\u0022target not connected: " + Target + "\u0022}";
                    await SendJson(Ws, Err);
                    CmdSenders.TryRemove(Id, out _);
                }
            }
            else if (Type == "log")
            {
                var Id = Root.GetProperty("id").GetString() ?? string.Empty;
                if (CmdSenders.TryGetValue(Id, out var Sender)) { await SendJson(Sender, Body); }
            }
            else if (Type == "result")
            {
                var Id = Root.GetProperty("id").GetString() ?? string.Empty;
                if (CmdSenders.TryRemove(Id, out var Sender))
                {
                    await SendJson(Sender, Body);
                    await Console.Out.WriteLineAsync("result " + Id + " -> sender");
                }
            }
        }
    }
    catch (Exception E)
    {
        await Console.Error.WriteLineAsync("peer err: " + E.Message);
    }
    finally
    {
        if (Name != null) Clients.TryRemove(Name, out _);
        try { Ws.Dispose(); } catch { }
    }
}

while (Listener.IsListening)
{
    var Ctx = await Listener.GetContextAsync();
    if (!Ctx.Request.IsWebSocketRequest) { Ctx.Response.StatusCode = 400; Ctx.Response.Close(); continue; }
    var WsCtx = await Ctx.AcceptWebSocketAsync(null);
    _ = Task.Run(() => HandlePeer(WsCtx.WebSocket));
}
return 0;
