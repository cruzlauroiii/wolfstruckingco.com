#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

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
var AudioDir = Get("AudioDir")!;
var FramesDir = Get("FramesDir")!;
var OutDir = Get("OutDir")!;
Directory.CreateDirectory(FramesDir);
Directory.CreateDirectory(OutDir);

var UserData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
var PortFile = Path.Combine(UserData, "DevToolsActivePort");
var PortLines = await File.ReadAllLinesAsync(PortFile);
var DebugPort = int.Parse(PortLines[0], CultureInfo.InvariantCulture);
var BrowserPath = PortLines.Length > 1 ? PortLines[1] : "/devtools/browser";
var BrowserWsUrl = "ws://127.0.0.1:" + DebugPort.ToString(CultureInfo.InvariantCulture) + BrowserPath;

using var Ws = new ClientWebSocket();
Ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
Ws.Options.SetRequestHeader("Origin", "http://localhost");
using var ConnectCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
await Ws.ConnectAsync(new Uri(BrowserWsUrl), ConnectCts.Token);

var CmdId = 0;
var Pending = new System.Collections.Concurrent.ConcurrentDictionary<int, TaskCompletionSource<JsonElement>>();
var Cts = new CancellationTokenSource();

async Task<JsonElement> SendAsync(string Method, object? Params = null, string? SessionId = null)
{
    var Id = Interlocked.Increment(ref CmdId);
    var Sb = new StringBuilder();
    Sb.Append('{').Append("\u0022id\u0022:").Append(Id.ToString(CultureInfo.InvariantCulture));
    Sb.Append(",\u0022method\u0022:\u0022").Append(Method).Append('\u0022');
    if (Params is not null) Sb.Append(",\u0022params\u0022:").Append(JsonSerializer.Serialize(Params));
    if (SessionId is not null) Sb.Append(",\u0022sessionId\u0022:\u0022").Append(SessionId).Append('\u0022');
    Sb.Append('}');
    var Tcs = new TaskCompletionSource<JsonElement>();
    Pending[Id] = Tcs;
    await Ws.SendAsync(Encoding.UTF8.GetBytes(Sb.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
    using var T = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    using var R = T.Token.Register(() => Tcs.TrySetCanceled());
    return await Tcs.Task;
}

_ = Task.Run(async () =>
{
    var Buf = new byte[4 << 20];
    while (!Cts.IsCancellationRequested && Ws.State == WebSocketState.Open)
    {
        var Sb = new StringBuilder();
        WebSocketReceiveResult Rx;
        do { Rx = await Ws.ReceiveAsync(Buf, Cts.Token); Sb.Append(Encoding.UTF8.GetString(Buf, 0, Rx.Count)); } while (!Rx.EndOfMessage);
        if (Rx.MessageType == WebSocketMessageType.Close) break;
        using var Jd = JsonDocument.Parse(Sb.ToString());
        var Root = Jd.RootElement;
        if (Root.TryGetProperty("id", out var IdEl) && Pending.TryRemove(IdEl.GetInt32(), out var Tcs1))
        {
            Tcs1.TrySetResult(Root.TryGetProperty("result", out var Res) ? Res.Clone() : default);
        }
    }
});

var Tgts = await SendAsync("Target.getTargets");
string? PageTargetId = null;
foreach (var T in Tgts.GetProperty("targetInfos").EnumerateArray())
{
    if (T.GetProperty("type").GetString() == "page")
    {
        var U = T.GetProperty("url").GetString() ?? string.Empty;
        if (!U.StartsWith("chrome://", StringComparison.Ordinal) && !U.StartsWith("devtools://", StringComparison.Ordinal)) { PageTargetId = T.GetProperty("targetId").GetString(); break; }
    }
}
PageTargetId ??= Tgts.GetProperty("targetInfos").EnumerateArray().FirstOrDefault(T => T.GetProperty("type").GetString() == "page").GetProperty("targetId").GetString();
if (string.IsNullOrEmpty(PageTargetId)) return 2;

var Att = await SendAsync("Target.attachToTarget", new { targetId = PageTargetId, flatten = true });
var Sid = Att.GetProperty("sessionId").GetString()!;

await SendAsync("Page.enable", null, Sid);
await SendAsync("Runtime.enable", null, Sid);
await SendAsync("Network.clearBrowserCache", null, Sid);

await SendAsync("Page.navigate", new { url = "https://cruzlauroiii.github.io/wolfstruckingco.com/Login/" }, Sid);
await Task.Delay(1500);
for (var Wi = 0; Wi < 25; Wi++)
{
    var R1 = await SendAsync("Runtime.evaluate", new { expression = "typeof window.WolfsInterop", returnByValue = true }, Sid);
    if (R1.TryGetProperty("result", out var Rs) && Rs.TryGetProperty("value", out var V) && V.GetString() == "object") break;
    await Task.Delay(400);
}
await SendAsync("Runtime.evaluate", new { expression = "localStorage.setItem('wolfs_session','demo-sess-' + Date.now()); localStorage.setItem('wolfs_role','user'); localStorage.setItem('wolfs_email','demo@wolfs.example'); localStorage.removeItem('wolfs_sso'); localStorage.setItem('wolfs_theme','light'); document.documentElement.setAttribute('data-theme','light'); 'primed'", returnByValue = true }, Sid);
await Task.Delay(300);

var Json = await File.ReadAllTextAsync(ScenesJsonPath);
using var Doc = JsonDocument.Parse(Json);
var Done = 0; var Failed = 0;
foreach (var Scene in Doc.RootElement.EnumerateArray())
{
    var Url = Scene.GetProperty("target").GetString() ?? string.Empty;
    var Pad = string.Empty;
    var CbIdx = Url.IndexOf("cb=", StringComparison.Ordinal);
    if (CbIdx >= 0) { var S = CbIdx + 3; var E = S; while (E < Url.Length && (char.IsDigit(Url[E]) || (Url[E] >= 'a' && Url[E] <= 'z'))) E++; Pad = Url[S..E]; }
    if (string.IsNullOrEmpty(Pad)) continue;
    var ChatMsg = Scene.TryGetProperty("chat", out var ChEl) ? ChEl.GetString() ?? string.Empty : string.Empty;
    var Mp4 = Path.Combine(OutDir, $"scene-{Pad}.mp4");
    var Png = Path.Combine(FramesDir, $"scene-{Pad}.png");
    var Mp3 = Path.Combine(AudioDir, $"scene-{Pad}.mp3");
    try
    {
        await SendAsync("Page.navigate", new { url = Url }, Sid);
        await Task.Delay(1500);
        for (var Wi = 0; Wi < 25; Wi++)
        {
            var R2 = await SendAsync("Runtime.evaluate", new { expression = "typeof window.WolfsInterop", returnByValue = true }, Sid);
            if (R2.TryGetProperty("result", out var Rs) && Rs.TryGetProperty("value", out var V) && V.GetString() == "object") break;
            await Task.Delay(400);
        }
        await SendAsync("Runtime.evaluate", new { expression = "localStorage.setItem('wolfs_theme','light'); document.documentElement.setAttribute('data-theme','light'); 'themed'", returnByValue = true }, Sid);
        await Task.Delay(400);
        if (!string.IsNullOrEmpty(ChatMsg))
        {
            var TypeJs = "const i=document.querySelector('.WChat input,.WChat textarea,input[type=text],textarea');if(i){i.focus();i.value=\u0022" + ChatMsg.Replace("'","\\'").Replace("\u0022","\\\u0022") + "\u0022;i.dispatchEvent(new Event('input',{bubbles:true}));const b=document.querySelector('.WChat button,button[type=submit]');if(b)b.click();}'typed'";
            await SendAsync("Runtime.evaluate", new { expression = TypeJs, returnByValue = true }, Sid);
            await Task.Delay(1500);
        }
        var Shot = await SendAsync("Page.captureScreenshot", new { format = "png", captureBeyondViewport = true }, Sid);
        var B64 = Shot.GetProperty("data").GetString() ?? string.Empty;
        var Bytes = Convert.FromBase64String(B64);
        await File.WriteAllBytesAsync(Png, Bytes);
        var Psi = new ProcessStartInfo("ffmpeg") { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
        var HasMp3 = File.Exists(Mp3);
        var FfArgs = HasMp3 ? new[] { "-y", "-loop", "1", "-i", Png, "-i", Mp3, "-c:v", "libx264", "-tune", "stillimage", "-c:a", "aac", "-shortest", "-pix_fmt", "yuv420p", "-vf", "scale=1920:1080,format=yuv420p", "-r", "24", Mp4 } : new[] { "-y", "-loop", "1", "-i", Png, "-c:v", "libx264", "-t", "3", "-pix_fmt", "yuv420p", "-vf", "scale=1920:1080,format=yuv420p", "-r", "24", Mp4 };
        foreach (var A in FfArgs) Psi.ArgumentList.Add(A);
        using var P = Process.Start(Psi)!;
        _ = await P.StandardError.ReadToEndAsync();
        await P.WaitForExitAsync();
        if (P.ExitCode == 0 && File.Exists(Mp4)) Done++; else Failed++;
        await Console.Error.WriteLineAsync("[" + Done.ToString(CultureInfo.InvariantCulture) + "] " + Pad + (P.ExitCode == 0 ? " ok" : " fail"));
    }
    catch (Exception E)
    {
        Failed++;
        await Console.Error.WriteLineAsync("[fail] " + Pad + " ex=" + E.Message);
    }
}
Cts.Cancel();
try { await Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None); } catch { }
await Console.Error.WriteLineAsync("captures-direct: done=" + Done.ToString(CultureInfo.InvariantCulture) + " failed=" + Failed.ToString(CultureInfo.InvariantCulture));
return Failed > 0 ? 4 : 0;
