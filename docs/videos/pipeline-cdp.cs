using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VideoPipeline;

internal sealed class CdpClient : IAsyncDisposable
{
    private ClientWebSocket Ws = new();
    private readonly ConcurrentQueue<JsonNode> EventQueue = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonNode>> Pending = new();
    private CancellationTokenSource? ReceiveCts;
    private Task? ReceiveLoop;
    private int CmdId;
    private string? AttachedSessionId;

    public async Task ConnectAndAttachAsync()
    {
        var ChromeUserDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
        var ActivePortFile = Path.Combine(ChromeUserDataDir, "DevToolsActivePort");
        if (!File.Exists(ActivePortFile)) { throw new InvalidOperationException("DevToolsActivePort not found - is Chrome running with remote debugging?"); }
        var PortLines = await File.ReadAllLinesAsync(ActivePortFile);
        var CdpPort = int.Parse(PortLines[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        var BrowserPath = PortLines.Length >= 2 ? PortLines[1].Trim() : "/devtools/browser";
        var BrowserWsUrl = $"ws://127.0.0.1:{CdpPort.ToString(System.Globalization.CultureInfo.InvariantCulture)}{BrowserPath}";
        FocusChrome();
        await Task.Delay(300);
        using var DeadlineCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var ConnectTask = Ws.ConnectAsync(new Uri(BrowserWsUrl), DeadlineCts.Token);
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500);
            if (!ConnectTask.IsCompleted) { FocusChrome(); await Task.Delay(150); ClickAtPoint(638, 287); }
        });
        await ConnectTask;
        if (Ws.State != WebSocketState.Open) { throw new InvalidOperationException($"WS not open: {Ws.State}"); }

        ReceiveCts = new CancellationTokenSource();
        ReceiveLoop = Task.Run(() => RunReceiveLoopAsync(ReceiveCts.Token));

        var ExistingTargets = await SendAsync("Target.getTargets");
        var Infos = ExistingTargets["result"]?["targetInfos"]?.AsArray();
        string? AttachTargetId = null;
        if (Infos != null)
        {
            foreach (var T in Infos)
            {
                if (T?["type"]?.GetValue<string>() == "page")
                {
                    AttachTargetId = T["targetId"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(AttachTargetId)) { break; }
                }
            }
        }
        if (string.IsNullOrEmpty(AttachTargetId))
        {
            var CreateResp = await SendAsync("Target.createTarget", new { url = "about:blank" });
            AttachTargetId = CreateResp["result"]?["targetId"]?.GetValue<string>();
            if (string.IsNullOrEmpty(AttachTargetId)) { throw new InvalidOperationException($"Target.createTarget returned no targetId: {CreateResp.ToJsonString()}"); }
        }
        var AttachResp = await SendAsync("Target.attachToTarget", new { targetId = AttachTargetId, flatten = true });
        AttachedSessionId = AttachResp["result"]?["sessionId"]?.GetValue<string>();
        if (string.IsNullOrEmpty(AttachedSessionId)) { throw new InvalidOperationException($"Target.attachToTarget returned no sessionId: {AttachResp.ToJsonString()}"); }

        var WinResp = await SendAsync("Browser.getWindowForTarget", new { targetId = AttachTargetId });
        var WindowId = WinResp["result"]?["windowId"]?.GetValue<int>() ?? 0;
        if (WindowId > 0)
        {
            try { await SendAsync("Browser.setWindowBounds", new { windowId = WindowId, bounds = new { windowState = "normal" } }); } catch { }
            try { await SendAsync("Browser.setWindowBounds", new { windowId = WindowId, bounds = new { windowState = "maximized" } }); } catch { }
        }

        await SendAsync("Page.enable");
        await SendAsync("Runtime.enable");
        await SendAsync("Network.enable");
        await SendAsync("Network.setCacheDisabled", new { cacheDisabled = true });
        await SendAsync("Emulation.setDeviceMetricsOverride", new { width = 393, height = 852, deviceScaleFactor = 3, mobile = true, screenOrientation = new { angle = 0, type = "portraitPrimary" } });
        await SendAsync("Emulation.setTouchEmulationEnabled", new { enabled = true });

        await Task.Delay(800);
        DismissInfobarOnce();
    }

    public Task<JsonNode> SendAsync(string Method, object? Params = null) => SendOnceAsync(Method, Params, 30);
    public Task<JsonNode> SendOnceAsync(string Method, object? Params, int TimeoutSec) => SendOnceAsyncImpl(Method, Params, TimeoutSec);

    private async Task<JsonNode> SendOnceAsyncImpl(string Method, object? Params, int TimeoutSec)
    {
        var Id = Interlocked.Increment(ref CmdId);
        var Tcs = new TaskCompletionSource<JsonNode>(TaskCreationOptions.RunContinuationsAsynchronously);
        Pending[Id] = Tcs;
        var Msg = new JsonObject { ["id"] = Id, ["method"] = Method };
        if (Params != null) { Msg["params"] = JsonNode.Parse(JsonSerializer.Serialize(Params)); }
        if (AttachedSessionId != null && Method != "Target.getTargets" && Method != "Target.attachToTarget" && Method != "Target.createTarget" && Method != "Target.closeTarget" && Method != "Browser.getWindowForTarget" && Method != "Browser.setWindowBounds") { Msg["sessionId"] = AttachedSessionId; }
        var Bytes = Encoding.UTF8.GetBytes(Msg.ToJsonString());
        await Ws.SendAsync(Bytes, WebSocketMessageType.Text, true, default);
        using var Timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSec));
        Timeout.Token.Register(() => { Pending.TryRemove(Id, out _); Tcs.TrySetException(new TimeoutException($"CDP {Method} timed out (id={Id})")); });
        return await Tcs.Task;
    }

    public async Task<JsonNode> WaitForEventAsync(string Method, int TimeoutSec = 30)
    {
        var Deadline = DateTimeOffset.UtcNow.AddSeconds(TimeoutSec);
        while (DateTimeOffset.UtcNow < Deadline)
        {
            while (EventQueue.TryDequeue(out var Ev))
            {
                if (Ev["method"]?.GetValue<string>() == Method) { return Ev; }
            }
            await Task.Delay(50);
        }
        throw new TimeoutException($"CDP event {Method} not received in {TimeoutSec.ToString(System.Globalization.CultureInfo.InvariantCulture)}s");
    }

    public void DrainEvents() { while (EventQueue.TryDequeue(out _)) { } }

    private async Task RunReceiveLoopAsync(CancellationToken Ct)
    {
        var Buf = new byte[1 << 22];
        while (!Ct.IsCancellationRequested && Ws.State == WebSocketState.Open)
        {
            var Sb = new StringBuilder();
            try
            {
                WebSocketReceiveResult R;
                do { R = await Ws.ReceiveAsync(Buf, Ct); Sb.Append(Encoding.UTF8.GetString(Buf, 0, R.Count)); if (R.MessageType == WebSocketMessageType.Close) { return; } } while (!R.EndOfMessage);
            }
            catch (OperationCanceledException) { return; }
            catch (WebSocketException) { return; }
            JsonNode? Doc;
            try { Doc = JsonNode.Parse(Sb.ToString()); } catch { continue; }
            if (Doc is null) { continue; }
            var IdN = Doc["id"];
            if (IdN is not null && Pending.TryRemove(IdN.GetValue<int>(), out var Tcs)) { Tcs.TrySetResult(Doc); continue; }
            if (Doc["method"] is not null) { EventQueue.Enqueue(Doc); }
        }
    }

    public Task DismissInfobarAsync() { DismissInfobarOnce(); return Task.CompletedTask; }

    private static void DismissInfobarOnce()
    {
        try { ClickAtPoint(1872, 192); } catch { }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private static void FocusChrome()
    {
        var H = FindWindow("Chrome_WidgetWin_1", null);
        if (H != IntPtr.Zero) { SetForegroundWindow(H); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    private static void ClickAtPoint(int X, int Y)
    {
        const int SM_CXSCREEN = 0;
        const int SM_CYSCREEN = 1;
        const uint INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        var W = GetSystemMetrics(SM_CXSCREEN);
        var H = GetSystemMetrics(SM_CYSCREEN);
        var AbsX = (int)((X * 65535.0) / W);
        var AbsY = (int)((Y * 65535.0) / H);
        SetCursorPos(X, Y);
        var Inputs = new INPUT[]
        {
            new() { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = AbsX, dy = AbsY, dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE } } },
            new() { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = AbsX, dy = AbsY, dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE } } },
            new() { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = AbsX, dy = AbsY, dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE } } },
        };
        SendInput((uint)Inputs.Length, Inputs, Marshal.SizeOf<INPUT>());
    }

    public async ValueTask DisposeAsync()
    {
        try { ReceiveCts?.Cancel(); } catch { }
        if (ReceiveLoop != null) { try { await ReceiveLoop; } catch { } }
        try { if (Ws.State == WebSocketState.Open) { await Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", default); } } catch { }
        Ws.Dispose();
        ReceiveCts?.Dispose();
    }
}
