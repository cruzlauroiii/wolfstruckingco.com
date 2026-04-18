#:property TargetFramework=net11.0-windows
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
int IntFromConfig(string Name, int Default)
{
    var M = Regex.Match(Body, @"const\s+int\s+" + Name + @"\s*=\s*(?<v>-?\d+)\s*;", RegexOptions.ExplicitCapture);
    return M.Success ? int.Parse(M.Groups["v"].Value, System.Globalization.CultureInfo.InvariantCulture) : Default;
}
string StringFromConfig(string Name, string Default)
{
    var M = Regex.Match(Body, @"const\s+string\s+" + Name + "\\s*=\\s*\"(?<v>[^\"]*)\"\\s*;", RegexOptions.ExplicitCapture);
    return M.Success ? M.Groups["v"].Value : Default;
}

var Width = IntFromConfig("Width", 540);
var Height = IntFromConfig("Height", 960);
var DeviceScaleFactor = IntFromConfig("DeviceScaleFactor", 2);
var TimeoutSeconds = IntFromConfig("TimeoutSeconds", 30);
var AllowClickX = IntFromConfig("AllowClickX", 638);
var AllowClickY = IntFromConfig("AllowClickY", 287);
var InfobarClickX = IntFromConfig("InfobarClickX", 1872);
var InfobarClickY = IntFromConfig("InfobarClickY", 192);
var AllowClickDelayMs = IntFromConfig("AllowClickDelayMs", 1500);
var ChromeUserDataSubpath = StringFromConfig("ChromeUserDataSubpath", "Google\\Chrome\\User Data");

var ChromeUserDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
foreach (var Part in ChromeUserDataSubpath.Split('\\')) { ChromeUserDataDir = Path.Combine(ChromeUserDataDir, Part); }
var ActivePortFile = Path.Combine(ChromeUserDataDir, "DevToolsActivePort");
if (!File.Exists(ActivePortFile)) { await Console.Error.WriteLineAsync("DevToolsActivePort not found"); return 3; }

var PortLines = await File.ReadAllLinesAsync(ActivePortFile);
if (PortLines.Length < 1) { await Console.Error.WriteLineAsync("DevToolsActivePort empty"); return 4; }
var Port = int.Parse(PortLines[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
var BrowserPath = PortLines.Length >= 2 ? PortLines[1].Trim() : "/devtools/browser";
var WsUrl = $"ws://127.0.0.1:{Port.ToString(System.Globalization.CultureInfo.InvariantCulture)}{BrowserPath}";

FocusChrome();
await Task.Delay(300);

using var Ws = new ClientWebSocket();
using var Cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

var ConnectTask = Ws.ConnectAsync(new Uri(WsUrl), Cts.Token);
_ = Task.Run(async () =>
{
    await Task.Delay(AllowClickDelayMs);
    if (!ConnectTask.IsCompleted) { FocusChrome(); await Task.Delay(150); ClickAt(AllowClickX, AllowClickY); }
});

try { await ConnectTask; }
catch (Exception Ex) { await Console.Error.WriteLineAsync($"connect failed: {Ex.Message}"); return 5; }

if (Ws.State != WebSocketState.Open) { await Console.Error.WriteLineAsync($"WS not open: {Ws.State}"); return 6; }

var ReceiveBuf = new byte[1 << 20];
var Pending = new System.Collections.Concurrent.ConcurrentDictionary<int, TaskCompletionSource<JsonNode>>();
var ReceiveCts = new CancellationTokenSource();
var ReceiveLoop = Task.Run(async () =>
{
    while (!ReceiveCts.IsCancellationRequested && Ws.State == WebSocketState.Open)
    {
        var Sb = new StringBuilder();
        try
        {
            WebSocketReceiveResult R;
            do { R = await Ws.ReceiveAsync(ReceiveBuf, ReceiveCts.Token); Sb.Append(Encoding.UTF8.GetString(ReceiveBuf, 0, R.Count)); if (R.MessageType == WebSocketMessageType.Close) { return; } } while (!R.EndOfMessage);
        }
        catch { return; }
        JsonNode? Doc;
        try { Doc = JsonNode.Parse(Sb.ToString()); } catch { continue; }
        if (Doc?["id"] is JsonNode IdN && Pending.TryRemove(IdN.GetValue<int>(), out var Tcs)) { Tcs.TrySetResult(Doc); }
    }
});

var Id = 0;
async Task<JsonNode> SendAsync(string Method, object? Params = null, string? SessionId = null)
{
    Id++;
    var Tcs = new TaskCompletionSource<JsonNode>(TaskCreationOptions.RunContinuationsAsynchronously);
    Pending[Id] = Tcs;
    var Msg = new JsonObject { ["id"] = Id, ["method"] = Method };
    if (Params != null) { Msg["params"] = JsonNode.Parse(JsonSerializer.Serialize(Params)); }
    if (SessionId != null) { Msg["sessionId"] = SessionId; }
    var Bytes = Encoding.UTF8.GetBytes(Msg.ToJsonString());
    await Ws.SendAsync(Bytes, WebSocketMessageType.Text, true, default);
    using var T = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    T.Token.Register(() => { Pending.TryRemove(Id, out _); Tcs.TrySetException(new TimeoutException(Method)); });
    return await Tcs.Task;
}

try
{
    var Version = await SendAsync("Browser.getVersion");
    var Product = Version["result"]?["product"]?.GetValue<string>() ?? "";
    if (string.IsNullOrEmpty(Product)) { await Console.Error.WriteLineAsync("no Browser.getVersion product"); return 7; }

    var TargetsResp = await SendAsync("Target.getTargets");
    var Infos = TargetsResp["result"]?["targetInfos"]?.AsArray();
    string? PageTargetId = null;
    if (Infos != null)
    {
        foreach (var T in Infos) { if (T?["type"]?.GetValue<string>() == "page") { PageTargetId = T["targetId"]?.GetValue<string>(); break; } }
    }
    if (PageTargetId == null)
    {
        var Created = await SendAsync("Target.createTarget", new { url = "about:blank" });
        PageTargetId = Created["result"]?["targetId"]?.GetValue<string>();
    }
    if (string.IsNullOrEmpty(PageTargetId)) { await Console.Error.WriteLineAsync("no page target"); return 8; }

    var Win = await SendAsync("Browser.getWindowForTarget", new { targetId = PageTargetId });
    var WindowId = Win["result"]?["windowId"]?.GetValue<int>() ?? 0;
    if (WindowId > 0)
    {
        await SendAsync("Browser.setWindowBounds", new { windowId = WindowId, bounds = new { windowState = "normal" } });
        await SendAsync("Browser.setWindowBounds", new { windowId = WindowId, bounds = new { windowState = "maximized" } });
    }

    var Attach = await SendAsync("Target.attachToTarget", new { targetId = PageTargetId, flatten = true });
    var SessionId = Attach["result"]?["sessionId"]?.GetValue<string>();
    if (!string.IsNullOrEmpty(SessionId))
    {
        await SendAsync("Page.enable", null, SessionId);
        await SendAsync("Emulation.setDeviceMetricsOverride", new { width = Width, height = Height, deviceScaleFactor = DeviceScaleFactor, mobile = true, screenOrientation = new { angle = 0, type = "portraitPrimary" } }, SessionId);
        await SendAsync("Emulation.setTouchEmulationEnabled", new { enabled = true }, SessionId);

        await Task.Delay(800);
        ClickAt(InfobarClickX, InfobarClickY);

        await SendAsync("Page.navigate", new { url = "https://cruzlauroiii.github.io/wolfstruckingco.com/?cb=verify" }, SessionId);
        await Task.Delay(5000);
        var Layout = await SendAsync("Page.getLayoutMetrics", null, SessionId);
        await Console.Error.WriteLineAsync($"layoutMetrics: {Layout.ToJsonString()}");
        var Shot = await SendAsync("Page.captureScreenshot", new { format = "png" }, SessionId);
        var B64 = Shot["result"]?["data"]?.GetValue<string>();
        if (B64 != null)
        {
            var Out = Path.Combine(Path.GetTempPath(), "wolfs-verify-shot.png");
            await File.WriteAllBytesAsync(Out, Convert.FromBase64String(B64));
            await Console.Error.WriteLineAsync($"saved {Out} {new FileInfo(Out).Length} bytes");
        }
        else
        {
            await Console.Error.WriteLineAsync($"shot returned no data: {Shot.ToJsonString()}");
        }
    }
}
catch (Exception Ex)
{
    await Console.Error.WriteLineAsync($"verify error: {Ex.Message}");
    return 9;
}
finally
{
    ReceiveCts.Cancel();
    try { if (Ws.State == WebSocketState.Open) { await Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", default); } } catch { }
}

return 0;

static void FocusChrome()
{
    var H = NativeMethods.FindWindow("Chrome_WidgetWin_1", null);
    if (H != IntPtr.Zero) { NativeMethods.SetForegroundWindow(H); }
}

static void ClickAt(int X, int Y)
{
    const int SM_CXSCREEN = 0;
    const int SM_CYSCREEN = 1;
    const uint INPUT_MOUSE = 0;
    const uint MOUSEEVENTF_MOVE = 0x0001;
    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;
    const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    var W = NativeMethods.GetSystemMetrics(SM_CXSCREEN);
    var H = NativeMethods.GetSystemMetrics(SM_CYSCREEN);
    var AbsX = (int)((X * 65535.0) / W);
    var AbsY = (int)((Y * 65535.0) / H);
    NativeMethods.SetCursorPos(X, Y);
    var Inputs = new NativeMethods.INPUT[]
    {
        new() { type = INPUT_MOUSE, U = new NativeMethods.InputUnion { mi = new NativeMethods.MOUSEINPUT { dx = AbsX, dy = AbsY, dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE } } },
        new() { type = INPUT_MOUSE, U = new NativeMethods.InputUnion { mi = new NativeMethods.MOUSEINPUT { dx = AbsX, dy = AbsY, dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE } } },
        new() { type = INPUT_MOUSE, U = new NativeMethods.InputUnion { mi = new NativeMethods.MOUSEINPUT { dx = AbsX, dy = AbsY, dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE } } },
    };
    NativeMethods.SendInput((uint)Inputs.Length, Inputs, Marshal.SizeOf<NativeMethods.INPUT>());
}

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT { public uint type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
}
