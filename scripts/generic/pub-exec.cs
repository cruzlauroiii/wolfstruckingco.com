#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
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

var ServerUrl = Get("ServerUrl")!;
var Target = Get("Target")!;
var Action = Get("Action") ?? "echo";
var Generic = Get("Generic") ?? string.Empty;
var Config = Get("Config") ?? string.Empty;
var EchoText = Get("EchoText") ?? string.Empty;
var Workdir = Get("Workdir") ?? string.Empty;

static string Esc(string S)
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

static async Task<string> ReceiveJson(ClientWebSocket Ws)
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

using var Ws = new ClientWebSocket();
await Ws.ConnectAsync(new Uri(ServerUrl), CancellationToken.None);

var Id = Guid.NewGuid().ToString("N");
string ArgsJson;
if (Action == "dotnet_run")
{
    ArgsJson = "{\u0022generic\u0022:\u0022" + Esc(Generic) + "\u0022,\u0022config\u0022:\u0022" + Esc(Config) + "\u0022,\u0022workdir\u0022:\u0022" + Esc(Workdir) + "\u0022}";
}
else
{
    ArgsJson = "{\u0022text\u0022:\u0022" + Esc(EchoText) + "\u0022}";
}
var Cmd = "{\u0022type\u0022:\u0022cmd\u0022,\u0022id\u0022:\u0022" + Id + "\u0022,\u0022target\u0022:\u0022" + Esc(Target) + "\u0022,\u0022action\u0022:\u0022" + Esc(Action) + "\u0022,\u0022args\u0022:" + ArgsJson + "}";
await Ws.SendAsync(Encoding.UTF8.GetBytes(Cmd), WebSocketMessageType.Text, true, CancellationToken.None);

using var Timeout = new CancellationTokenSource(TimeSpan.FromMinutes(15));
while (Ws.State == WebSocketState.Open && !Timeout.Token.IsCancellationRequested)
{
    var Body = await ReceiveJson(Ws);
    if (string.IsNullOrEmpty(Body)) break;
    using var Doc = JsonDocument.Parse(Body);
    var Root = Doc.RootElement;
    if (Root.GetProperty("type").GetString() == "result" && Root.GetProperty("id").GetString() == Id)
    {
        var ExitCode = Root.GetProperty("exit_code").GetInt32();
        var Stdout = Root.TryGetProperty("stdout", out var So) ? So.GetString() ?? string.Empty : string.Empty;
        var Stderr = Root.TryGetProperty("stderr", out var Se) ? Se.GetString() ?? string.Empty : string.Empty;
        if (!string.IsNullOrEmpty(Stdout)) await Console.Out.WriteAsync(Stdout);
        if (!string.IsNullOrEmpty(Stderr)) await Console.Error.WriteAsync(Stderr);
        try { await Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None); } catch { }
        return ExitCode;
    }
}
return 99;
