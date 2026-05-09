#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
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
var ClientName = Get("ClientName")!;
var Workdir = Get("Workdir") ?? Environment.CurrentDirectory;

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

static async Task SendJson(ClientWebSocket Ws, string Body)
{
    var Bytes = Encoding.UTF8.GetBytes(Body);
    await Ws.SendAsync(Bytes, WebSocketMessageType.Text, true, CancellationToken.None);
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
Ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
await Ws.ConnectAsync(new Uri(ServerUrl), CancellationToken.None);
await SendJson(Ws, "{\u0022type\u0022:\u0022subscribe\u0022,\u0022client\u0022:\u0022" + Esc(ClientName) + "\u0022}");
await Console.Out.WriteLineAsync("client " + ClientName + " connected to " + ServerUrl);

while (Ws.State == WebSocketState.Open)
{
    var Body = await ReceiveJson(Ws);
    if (string.IsNullOrEmpty(Body)) break;
    using var Doc = JsonDocument.Parse(Body);
    var Root = Doc.RootElement;
    var Type = Root.GetProperty("type").GetString() ?? string.Empty;
    if (Type != "cmd") continue;
    var Id = Root.GetProperty("id").GetString() ?? string.Empty;
    var Action = Root.GetProperty("action").GetString() ?? string.Empty;
    var ArgsEl = Root.GetProperty("args");
    await Console.Out.WriteLineAsync("cmd " + Id + " action=" + Action);
    var ExitCode = 0;
    var Stdout = string.Empty;
    var Stderr = string.Empty;
    try
    {
        if (Action == "echo")
        {
            Stdout = ArgsEl.TryGetProperty("text", out var T) ? T.GetString() ?? string.Empty : string.Empty;
        }
        else if (Action == "dotnet_run")
        {
            var Generic = ArgsEl.GetProperty("generic").GetString() ?? string.Empty;
            var Config = ArgsEl.GetProperty("config").GetString() ?? string.Empty;
            var Wd = ArgsEl.TryGetProperty("workdir", out var W) ? W.GetString() ?? Workdir : Workdir;
            var Psi = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Wd,
            };
            Psi.ArgumentList.Add("run");
            Psi.ArgumentList.Add(Generic);
            Psi.ArgumentList.Add(Config);
            using var P = Process.Start(Psi)!;
            async Task Stream(StreamReader Rd, string StreamName)
            {
                string? L;
                while ((L = await Rd.ReadLineAsync()) != null)
                {
                    var Msg = "{\u0022type\u0022:\u0022log\u0022,\u0022id\u0022:\u0022" + Esc(Id) + "\u0022,\u0022client\u0022:\u0022" + Esc(ClientName) + "\u0022,\u0022stream\u0022:\u0022" + StreamName + "\u0022,\u0022line\u0022:\u0022" + Esc(L) + "\u0022}";
                    await SendJson(Ws, Msg);
                }
            }
            await Task.WhenAll(Stream(P.StandardOutput, "stdout"), Stream(P.StandardError, "stderr"));
            await P.WaitForExitAsync();
            ExitCode = P.ExitCode;
        }
        else
        {
            ExitCode = 1;
            Stderr = "unknown action: " + Action;
        }
    }
    catch (Exception E)
    {
        ExitCode = 1;
        Stderr = E.GetType().Name + ": " + E.Message;
    }
    var Result = "{\u0022type\u0022:\u0022result\u0022,\u0022id\u0022:\u0022" + Esc(Id) + "\u0022,\u0022client\u0022:\u0022" + Esc(ClientName) + "\u0022,\u0022exit_code\u0022:" + ExitCode.ToString(CultureInfo.InvariantCulture) + ",\u0022stdout\u0022:\u0022" + Esc(Stdout) + "\u0022,\u0022stderr\u0022:\u0022" + Esc(Stderr) + "\u0022}";
    await SendJson(Ws, Result);
}
return 0;
