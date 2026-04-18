#:property TargetFramework=net11.0-windows
#:property UseWindowsForms=true
#:property UseWPF=true
#:property PublishTrimmed=false
#:property IsTrimmable=false
#:property PublishAot=false
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

var ChromeUserDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
var ActivePortFile = Path.Combine(ChromeUserDataDir, "DevToolsActivePort");
var Lines = await File.ReadAllLinesAsync(ActivePortFile);
var Port = int.Parse(Lines[0].Trim());
var Path1 = Lines.Length >= 2 ? Lines[1].Trim() : "/devtools/browser";
var Url = $"ws://127.0.0.1:{Port}{Path1}";

using var Ws = new ClientWebSocket();
Ws.Options.SetRequestHeader("Origin", "http://localhost");
await Ws.ConnectAsync(new Uri(Url), default);

var Buf = new byte[1 << 22];
int CmdId = 0;
async Task<JsonNode> SendNoSession(string Method, object? Params = null)
{
    CmdId++;
    var Msg = new JsonObject { ["id"] = CmdId, ["method"] = Method };
    if (Params != null) { Msg["params"] = JsonNode.Parse(JsonSerializer.Serialize(Params)); }
    var Bytes = Encoding.UTF8.GetBytes(Msg.ToJsonString());
    await Ws.SendAsync(Bytes, WebSocketMessageType.Text, true, default);
    while (true)
    {
        var Sb = new StringBuilder();
        WebSocketReceiveResult R;
        do { R = await Ws.ReceiveAsync(Buf, default); Sb.Append(Encoding.UTF8.GetString(Buf, 0, R.Count)); } while (!R.EndOfMessage);
        var Doc = JsonNode.Parse(Sb.ToString())!;
        if (Doc["id"]?.GetValue<int>() == CmdId) { return Doc; }
    }
}

string Sid = "";
async Task<JsonNode> Send(string Method, object? Params = null)
{
    CmdId++;
    var Msg = new JsonObject { ["id"] = CmdId, ["method"] = Method };
    if (Params != null) { Msg["params"] = JsonNode.Parse(JsonSerializer.Serialize(Params)); }
    if (!string.IsNullOrEmpty(Sid)) { Msg["sessionId"] = Sid; }
    var Bytes = Encoding.UTF8.GetBytes(Msg.ToJsonString());
    await Ws.SendAsync(Bytes, WebSocketMessageType.Text, true, default);
    while (true)
    {
        var Sb = new StringBuilder();
        WebSocketReceiveResult R;
        do { R = await Ws.ReceiveAsync(Buf, default); Sb.Append(Encoding.UTF8.GetString(Buf, 0, R.Count)); } while (!R.EndOfMessage);
        var Doc = JsonNode.Parse(Sb.ToString())!;
        if (Doc["id"]?.GetValue<int>() == CmdId) { return Doc; }
    }
}

var Targets = await SendNoSession("Target.getTargets");
JsonNode? Picked = null;
foreach (var T in Targets["result"]!["targetInfos"]!.AsArray())
{
    var TyType = T!["type"]!.GetValue<string>();
    var TyUrl = T["url"]!.GetValue<string>();
    Console.WriteLine($"target: type={TyType} url={TyUrl}");
    if (TyType == "page" && TyUrl.Contains("wolfstruckingco")) { Picked = T; }
}
Picked ??= Targets["result"]!["targetInfos"]!.AsArray().First(T => T!["type"]!.GetValue<string>() == "page");
var TargetId = Picked["targetId"]!.GetValue<string>();
Console.WriteLine($"\nattaching to: {Picked["url"]}");

var Att = await SendNoSession("Target.attachToTarget", new { targetId = TargetId, flatten = true });
Sid = Att["result"]!["sessionId"]!.GetValue<string>();
Console.WriteLine($"sessionId: {Sid}");

await Send("Page.enable");
await Send("Runtime.enable");
await Send("Emulation.setDeviceMetricsOverride", new { width = 414, height = 896, deviceScaleFactor = 2, mobile = true });
await Send("Page.navigate", new { url = "https://cruzlauroiii.github.io/wolfstruckingco.com/?cb=debug" });
await Task.Delay(5000);

var Metrics = await Send("Page.getLayoutMetrics");
Console.WriteLine($"\nLayoutMetrics:\n{Metrics["result"]?.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}");

var Eval = await Send("Runtime.evaluate", new { expression = "JSON.stringify({iw: window.innerWidth, ih: window.innerHeight, sw: document.documentElement.scrollWidth, sh: document.documentElement.scrollHeight, dpr: window.devicePixelRatio, ua: navigator.userAgent.substring(0,80)})", returnByValue = true });
Console.WriteLine($"\nWindow Eval:\n{Eval["result"]?["result"]?["value"]?.GetValue<string>()}");

var Shot1 = await Send("Page.captureScreenshot", new { format = "png" });
var B1 = Shot1["result"]?["data"]?.GetValue<string>() ?? "";
Console.WriteLine($"\nNo-clip PNG bytes: {Convert.FromBase64String(B1).Length}");
File.WriteAllBytes(@"C:\Users\user1\AppData\Local\Temp\debug-noclip.png", Convert.FromBase64String(B1));

var Shot2 = await Send("Page.captureScreenshot", new { format = "png", captureBeyondViewport = true });
var B2 = Shot2["result"]?["data"]?.GetValue<string>() ?? "";
Console.WriteLine($"BeyondViewport PNG bytes: {Convert.FromBase64String(B2).Length}");
File.WriteAllBytes(@"C:\Users\user1\AppData\Local\Temp\debug-beyond.png", Convert.FromBase64String(B2));

return 0;
