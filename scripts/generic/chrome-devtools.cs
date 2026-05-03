#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:property TargetFramework=net11.0-windows
#:property UseWindowsForms=true
#:property UseWPF=true
#:property PublishAot=false
#:property AllowUnsafeBlocks=true
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:include CdpCommands.cs
#:include CdpConstants.cs
#:include CdpScratchConfig.cs
#:include CdpSetup.cs
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CdpTool;
using File = System.IO.File;
using Path = System.IO.Path;

await new CdpCli().RunAsync(args);

namespace CdpTool
{
public sealed partial class CdpCli
{
    private static readonly string ChromeUserDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
    private static readonly string ActivePortFile = Path.Combine(ChromeUserDataDir, "DevToolsActivePort");

    private ClientWebSocket? WebSocket;
    private string? SessionId;
    private int CommandId = 1;
    private readonly List<JsonNode> EventBuffer = [];

    private const int ServePort = 9333;

    public async Task RunAsync(string[] Argv)
    {
        if (Argv.Length == 0 || Argv[0] is "--help" or "-h" or "help")
        {
            PrintHelp();
            return;
        }

        var SilentMode = false;
        string? OutputPath = null;
        if (Argv.Length == 1 && Argv[0].EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(Argv[0]))
        {
            SilentMode = true;
            var Body = await File.ReadAllTextAsync(Argv[0]);
            var OutMatch = System.Text.RegularExpressions.Regex.Match(Body, "const\\s+string\\s+OutputPath\\s*=\\s*@?\"(?<v>[^\"]*)\"");
            if (OutMatch.Success) { OutputPath = OutMatch.Groups["v"].Value; }
            Argv = ScratchConfigParser.Expand(Argv[0]);
        }
        var SilentBuf = SilentMode ? new System.IO.StringWriter() : null;
        var OriginalOut = Console.Out;
        var OriginalErr = Console.Error;
        if (SilentBuf is not null) { Console.SetOut(SilentBuf); Console.SetError(SilentBuf); }

        var (Command, ParsedArgs) = ParseArgs(Argv);
        if (Command == "allow")
        {
            ClickAllowPrompt(ParsedArgs.ContainsKey("debug"));
            return;
        }

        if (Command == "screenshot_desktop")
        {
            ExecuteScreenshotDesktop(ParsedArgs);
            return;
        }

        if (Command == "focus_chrome")
        {
            FocusChrome();
            return;
        }

        if (Command == "navigate_address_bar")
        {
            NavigateAddressBar(ParsedArgs.TryGetValue(CdpKey.Url, out var NavUrl) ? NavUrl.ToString()! : string.Empty);
            return;
        }

        if (Command == "serve")
        {
            await RunServeModeAsync(ParsedArgs);
            return;
        }

        if (await TryForwardToServeAsync(Argv))
        {
            if (SilentBuf is not null)
            {
                Console.SetOut(OriginalOut);
                Console.SetError(OriginalErr);
                var Captured = SilentBuf.ToString();
                if (!string.IsNullOrEmpty(OutputPath))
                {
                    var FullOut = Path.IsPathRooted(OutputPath) ? OutputPath : Path.Combine(Environment.CurrentDirectory, OutputPath);
                    var Dir = Path.GetDirectoryName(FullOut);
                    if (!string.IsNullOrEmpty(Dir)) { Directory.CreateDirectory(Dir); }
                    await File.WriteAllTextAsync(FullOut, Captured);
                }
            }
            return;
        }

        await ConnectToChromeAsync(ParsedArgs);
        if (ParsedArgs.TryGetValue(CdpArg.PageId, out var GlobalPageId) && Command is not "select_page" and not "close_page")
        {
            var Pages = await GetPageTargetsAsync();
            var PageIndex = int.Parse(GlobalPageId.ToString()!, System.Globalization.CultureInfo.InvariantCulture) - 1;
            if (PageIndex >= 0 && PageIndex < Pages.Count)
            {
                await AttachToTargetAsync(Pages[PageIndex][CdpKey.TargetId]!.ToString());
            }
        }

        try
        {
            await DispatchCommandAsync(Command, ParsedArgs);
        }
        finally
        {
            WebSocket?.Dispose();
        }
        if (SilentBuf is not null)
        {
            Console.SetOut(OriginalOut);
            Console.SetError(OriginalErr);
            var Captured = SilentBuf.ToString();
            if (!string.IsNullOrEmpty(OutputPath))
            {
                var FullOut = Path.IsPathRooted(OutputPath) ? OutputPath : Path.Combine(Environment.CurrentDirectory, OutputPath);
                var Dir = Path.GetDirectoryName(FullOut);
                if (!string.IsNullOrEmpty(Dir)) { Directory.CreateDirectory(Dir); }
                await File.WriteAllTextAsync(FullOut, Captured);
            }
        }
    }

    private static async Task<bool> TrySendOnceAsync(string[] Argv)
    {
        try
        {
            using var Http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var Payload = string.Join(" ", Argv.Select(A => A.Contains(' ') ? $"\"{A}\"" : A));
            var Response = await Http.PostAsync($"http://127.0.0.1:{ServePort}/exec", new System.Net.Http.StringContent(Payload, Encoding.UTF8));
            Console.Write(await Response.Content.ReadAsStringAsync());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SpawnServeBackground()
    {
        try
        {
            var Cwd = Environment.CurrentDirectory;
            var Psi = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Cwd,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            Psi.ArgumentList.Add("run");
            Psi.ArgumentList.Add("main/scripts/generic/chrome-devtools.cs");
            Psi.ArgumentList.Add("main/scripts/specific/cdp-serve-scratch-config.cs");
            Process.Start(Psi);
        }
        catch
        {
        }
    }

    private static async Task<bool> TryForwardToServeAsync(string[] Argv)
    {
        if (await TrySendOnceAsync(Argv))
        {
            return true;
        }
        SpawnServeBackground();
        for (var I = 0; I < 30; I++)
        {
            await Task.Delay(500);
            if (await TrySendOnceAsync(Argv))
            {
                return true;
            }
        }
        return false;
    }

    private async Task RunServeModeAsync(Dictionary<string, object> ParsedArgs)
    {
        await ConnectToChromeAsync(ParsedArgs);
        var Listener = new System.Net.HttpListener();
        Listener.Prefixes.Add($"http://127.0.0.1:{ServePort}/");
        Listener.Start();
        Console.Error.WriteLine($"serve: connected, listening on http://127.0.0.1:{ServePort}");
        while (Listener.IsListening)
        {
            var Ctx = await Listener.GetContextAsync();
            var Req = Ctx.Request;
            var Res = Ctx.Response;
            if (Req.HttpMethod == "OPTIONS")
            {
                Res.StatusCode = 204;
                Res.Close();
                continue;
            }

            string Line;
            using (var Reader = new System.IO.StreamReader(Req.InputStream))
            {
                Line = (await Reader.ReadToEndAsync()).Trim();
            }

            var Output = new StringBuilder();
            var OldOut = Console.Out;
            var OldErr = Console.Error;
            Console.SetOut(new System.IO.StringWriter(Output));
            Console.SetError(new System.IO.StringWriter(Output));
            try
            {
                var LineArgs = SplitArgs(Line);
                var (Cmd, Args) = ParseArgs(LineArgs);
                if (WebSocket == null || WebSocket.State != System.Net.WebSockets.WebSocketState.Open)
                {
                    Output.AppendLine("serve: websocket not open, reconnecting...");
                    WebSocket?.Dispose();
                    WebSocket = null;
                    SessionId = null;
                    await ConnectToChromeAsync(Args);
                }
                if (Cmd == "allow")
                {
                    ClickAllowPrompt(Args.ContainsKey("debug"));
                }
                else if (Cmd == "screenshot_desktop")
                {
                    ExecuteScreenshotDesktop(Args);
                }
                else if (Cmd == "focus_chrome")
                {
                    FocusChrome();
                }
                else if (Cmd == "navigate_address_bar")
                {
                    NavigateAddressBar(Args.TryGetValue(CdpKey.Url, out var Nu) ? Nu.ToString()! : string.Empty);
                }
                else
                {
                    SessionId = null;
                    if (Args.TryGetValue(CdpArg.PageId, out var Pid) && Cmd is not "select_page" and not "close_page")
                    {
                        var Pages = await GetPageTargetsAsync();
                        var Idx = int.Parse(Pid.ToString()!, System.Globalization.CultureInfo.InvariantCulture) - 1;
                        if (Idx >= 0 && Idx < Pages.Count)
                        {
                            await AttachToTargetAsync(Pages[Idx][CdpKey.TargetId]!.ToString());
                        }
                    }

                    await DispatchCommandAsync(Cmd, Args);
                }
            }
            catch (Exception Ex)
            {
                Output.AppendLine($"Error: {Ex.Message}");
            }

            Console.SetOut(OldOut);
            Console.SetError(OldErr);
            var ResponseBytes = Encoding.UTF8.GetBytes(Output.ToString());
            Res.ContentType = "text/plain; charset=utf-8";
            Res.ContentLength64 = ResponseBytes.Length;
            await Res.OutputStream.WriteAsync(ResponseBytes);
            Res.Close();
        }
    }

    private static string[] SplitArgs(string Line)
    {
        var Args = new List<string>();
        var Current = new StringBuilder();
        var InQuote = false;
        var QuoteChar = '"';
        foreach (var Ch in Line)
        {
            if (InQuote)
            {
                if (Ch == QuoteChar)
                {
                    InQuote = false;
                }
                else
                {
                    Current.Append(Ch);
                }
            }
            else if (Ch is '"' or '\'')
            {
                InQuote = true;
                QuoteChar = Ch;
            }
            else if (Ch == ' ')
            {
                if (Current.Length > 0)
                {
                    Args.Add(Current.ToString());
                    Current.Clear();
                }
            }
            else
            {
                Current.Append(Ch);
            }
        }

        if (Current.Length > 0)
        {
            Args.Add(Current.ToString());
        }

        return [.. Args];
    }

    private async Task DispatchCommandAsync(string Command, Dictionary<string, object> ParsedArgs)
    {
        switch (Command)
        {
            case "list_pages":
                await ExecuteListPagesAsync();
                break;
            case "select_page":
                await ExecuteSelectPageAsync(ParsedArgs);
                break;
            case "close_page":
                await ExecuteClosePageAsync(ParsedArgs);
                break;
            case "new_page":
                {
                    var KeepUrl = ParsedArgs.TryGetValue(CdpKey.Url, out var KU) ? KU.ToString()! : "";
                    var Targets1 = await SendBrowserCommandAsync(Cdp.TargetGetTargets);
                    var Existing = Targets1![CdpKey.TargetInfos]!.AsArray()
                        .FirstOrDefault(T => T![CdpKey.Type]!.ToString() == CdpKey.Page
                            && !T![CdpKey.Url]!.ToString().StartsWith(CdpProto.ChromeScheme, StringComparison.Ordinal));
                    if (Existing != null && !string.IsNullOrEmpty(KeepUrl))
                    {
                        var KeptTid = Existing[CdpKey.TargetId]!.ToString();
                        await AttachToTargetAsync(KeptTid);
                        await SendCommandAsync(Cdp.PageNavigate, new JsonObject { [CdpKey.Url] = KeepUrl });
                        Console.WriteLine($"reused active tab; navigated to {KeepUrl}");
                    }
                    else
                    {
                        await ExecuteNewPageAsync(ParsedArgs);
                    }
                    DismissInfobar();
                }
                break;
            case "navigate_page":
                await ExecuteNavigatePageAsync(ParsedArgs);
                break;
            case "take_screenshot":
                await ExecuteTakeScreenshotAsync(ParsedArgs);
                break;
            case "take_snapshot":
                await ExecuteTakeSnapshotAsync(ParsedArgs);
                break;
            case "evaluate_script":
                await ExecuteEvaluateScriptAsync(ParsedArgs);
                break;
            case "click":
                await ExecuteClickAsync(ParsedArgs);
                break;
            case "hover":
                await ExecuteHoverAsync(ParsedArgs);
                break;
            case "fill":
                await ExecuteFillAsync(ParsedArgs);
                break;
            case "type_text":
                await ExecuteTypeTextAsync(ParsedArgs);
                break;
            case "press_key":
                await ExecutePressKeyAsync(ParsedArgs);
                break;
            case "list_console_messages":
                await ExecuteListConsoleMessagesAsync();
                break;
            case "list_network_requests":
                await ExecuteListNetworkRequestsAsync();
                break;
            case "resize_page":
                await ExecuteResizePageAsync(ParsedArgs);
                break;
            case "emulate":
                await ExecuteEmulateAsync(ParsedArgs);
                break;
            case "handle_dialog":
                await ExecuteHandleDialogAsync(ParsedArgs);
                break;
            case "drag":
                await ExecuteDragAsync(ParsedArgs);
                break;
            case "upload_file":
                await ExecuteUploadFileAsync(ParsedArgs);
                break;
            case "close_others":
                {
                    var Targets = await SendBrowserCommandAsync(Cdp.TargetGetTargets);
                    var AllPages = Targets![CdpKey.TargetInfos]!.AsArray()
                        .Where(T => T![CdpKey.Type]!.ToString() == CdpKey.Page)
                        .Select(T => T!)
                        .ToList();
                    var Keep = ParsedArgs.TryGetValue(CdpArg.PageId, out var KeepIdRaw)
                        ? int.Parse(KeepIdRaw.ToString()!, System.Globalization.CultureInfo.InvariantCulture) - 1
                        : 0;
                    var Closed = 0;
                    for (var I = 0; I < AllPages.Count; I++)
                    {
                        if (I == Keep) continue;
                        var Tid = AllPages[I][CdpKey.TargetId]!.ToString();
                        try { await SendBrowserCommandAsync("Target.closeTarget", new JsonObject { [CdpKey.TargetId] = Tid }); Closed++; } catch { }
                    }
                    Console.WriteLine($"closed {Closed} other tab(s) of {AllPages.Count} total; kept index {Keep + 1}");
                    break;
                }
            case "clear_cache":
                await SendCommandAsync("Network.clearBrowserCache");
                await SendCommandAsync("Network.clearBrowserCookies");
                Console.WriteLine("cache+cookies cleared");
                break;
            case "hard_reload":
                {
                    var Params = new System.Text.Json.Nodes.JsonObject { ["ignoreCache"] = true };
                    await SendCommandAsync(Cdp.PageReload, Params);
                    Console.WriteLine("hard reloaded");
                    break;
                }
            default:
                Console.Error.WriteLine($"{CdpMsg.UnknownCommand}{Command}");
                Environment.Exit(1);
                break;
        }
    }

    private static readonly int[] FallbackPorts = [9222, 9223, 9224, 9225, 9229, 9333];
    private const int DesktopDebugPort = 9222;

    private async Task ConnectToChromeAsync(Dictionary<string, object> ParsedArgs)
    {
        var TargetFilter = ParsedArgs.TryGetValue(CdpArg.Target, out var T) ? T.ToString()! : "desktop";
        var ExplicitPort = ParsedArgs.TryGetValue(CdpArg.Port, out var P) ? int.Parse(P.ToString()!, System.Globalization.CultureInfo.InvariantCulture) : (int?)null;
        if (Process.GetProcessesByName(CdpProto.ChromeProcessName).Length == 0)
        {
            Console.Error.WriteLine(CdpMsg.ChromeNotRunning);
            LaunchChromeWithDebugging(TargetFilter == "desktop" ? ExplicitPort ?? DesktopDebugPort : 0);
            await Task.Delay(8000);
        }
        else if (TargetFilter == "desktop" && await ResolveEndpointAsync("desktop", ExplicitPort) == null)
        {
            Console.Error.WriteLine("Desktop Chrome has no CDP port; relying on approval-mode auto-bind. NOT killing existing Chrome.");
            for (var Wait = 0; Wait < 6; Wait++)
            {
                await Task.Delay(2000);
                if (await ResolveEndpointAsync("desktop", ExplicitPort) != null) break;
                ClickAllowPrompt();
            }
        }

        for (var Attempt = 0; Attempt < 6; Attempt++)
        {
            var Endpoint = await ResolveEndpointAsync(TargetFilter, ExplicitPort);
            if (Endpoint == null)
            {
                ClickAllowPrompt();
                await Task.Delay(CdpTimeout.RetryDelayMs);
                continue;
            }

            Console.Error.WriteLine($"Connecting to {Endpoint}");
            WebSocket = new ClientWebSocket();
            using var Timeout = new CancellationTokenSource(CdpTimeout.ConnectTimeoutMs);
            var AllowCts = new CancellationTokenSource();
            var AllowTask = Task.Run(async () =>
            {
                while (!AllowCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(500);
                    ClickAllowPrompt();
                }
            });
            try
            {
                await WebSocket.ConnectAsync(new Uri(Endpoint), Timeout.Token);
                AllowCts.Cancel();
                if (WebSocket.State != System.Net.WebSockets.WebSocketState.Open)
                {
                    Console.Error.WriteLine($"WS not open after Allow click: {WebSocket.State}");
                    await Task.Delay(CdpTimeout.RetryDelayMs);
                    continue;
                }
                Console.Error.WriteLine("Connected!");
                if (!DismissInfobar())
                {
                    Thread.Sleep(800);
                    DismissInfobar();
                }
                return;
            }
            catch (Exception Ex)
            {
                AllowCts.Cancel();
                Console.Error.WriteLine($"Connect failed: {Ex.Message}");
                await Task.Delay(CdpTimeout.RetryDelayMs);
            }
        }

        Console.Error.WriteLine("Cannot connect. Enable: chrome://inspect/#remote-debugging");
        Environment.Exit(1);
    }

    private async Task<string?> ResolveEndpointAsync(string TargetFilter, int? ExplicitPort)
    {
        if (ExplicitPort == null && File.Exists(ActivePortFile))
        {
            var Lines = File.ReadAllLines(ActivePortFile).Where(L => !string.IsNullOrWhiteSpace(L)).ToArray();
            if (Lines.Length >= 2)
            {
                return $"{CdpProto.WsPrefix}{Lines[0].Trim()}{Lines[1].Trim()}";
            }
        }

        using var Http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var Ports = ExplicitPort != null ? [ExplicitPort.Value] : FallbackPorts;
        foreach (var Port in Ports)
        {
            try
            {
                var Json = await Http.GetStringAsync($"http://127.0.0.1:{Port}/json/version");
                var VersionNode = JsonNode.Parse(Json);
                var UserAgent = VersionNode?["User-Agent"]?.ToString() ?? string.Empty;
                var IsMobile = UserAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) || UserAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase);
                if (TargetFilter == "mobile" && !IsMobile)
                {
                    continue;
                }

                if (TargetFilter == "desktop" && IsMobile)
                {
                    continue;
                }

                var WsUrl = VersionNode?["webSocketDebuggerUrl"]?.ToString();
                if (WsUrl != null)
                {
                    return WsUrl;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static void LaunchChromeWithDebugging(int Port)
    {
        // Chrome 144+ approval-mode flow: when the user toggled chrome://inspect/#remote-debugging
        // on, Chrome's approval-mode HTTP server binds automatically via the
        // StartHttpServerInApprovalModeIfEnabled path — that path skips the
        // IsRemoteDebuggingAllowed default-data-dir check that blocks --remote-debugging-port.
        // So we DON'T pass --remote-debugging-port (that uses the blocked path); we just launch
        // Chrome maximized and Chrome itself binds the port + writes DevToolsActivePort. Each
        // CDP WebSocket connection prompts an Allow dialog — auto-clicked by ClickAllowPrompt.
        var ChromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe");
        var StartInfo = new ProcessStartInfo
        {
            FileName = ChromePath,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Maximized,
        };
        StartInfo.ArgumentList.Add("--start-maximized");
        StartInfo.ArgumentList.Add("--remote-allow-origins=*");
        StartInfo.ArgumentList.Add("--disable-features=SessionCrashedBubble,InfoBars");
        StartInfo.ArgumentList.Add("--no-first-run");
        StartInfo.ArgumentList.Add("--no-default-browser-check");
        StartInfo.ArgumentList.Add("--hide-crash-restore-bubble");
        StartInfo.ArgumentList.Add("--disable-session-crashed-bubble");
        StartInfo.ArgumentList.Add("--restore-last-session=false");
        Process.Start(StartInfo);
    }

    internal async Task<JsonNode?> SendCommandAsync(string Method, JsonObject? Params = null)
    {
        var Id = CommandId++;
        var Message = new JsonObject { [CdpKey.Id] = Id, [CdpKey.Method] = Method };
        if (Params != null)
        {
            Message[CdpKey.Params] = Params;
        }

        if (SessionId != null)
        {
            Message[CdpKey.SessionId] = SessionId;
        }

        await WebSocket!.SendAsync(Encoding.UTF8.GetBytes(Message.ToJsonString()), WebSocketMessageType.Text, true, CancellationToken.None);
        var Buffer = new byte[CdpTimeout.BufferSize];
        var Builder = new StringBuilder();
        while (true)
        {
            var Result = await WebSocket.ReceiveAsync(Buffer, CancellationToken.None);
            Builder.Append(Encoding.UTF8.GetString(Buffer, 0, Result.Count));
            if (Result.EndOfMessage)
            {
                var Parsed = JsonNode.Parse(Builder.ToString());
                if (Parsed?[CdpKey.Id]?.GetValue<int>() == Id)
                {
                    return Parsed?[CdpKey.Result];
                }

                if (Parsed?[CdpKey.Method] != null)
                {
                    EventBuffer.Add(Parsed!);
                }

                Builder.Clear();
            }
        }
    }

    internal async Task<JsonNode?> SendBrowserCommandAsync(string Method, JsonObject? Params = null)
    {
        var SavedSession = SessionId;
        SessionId = null;
        var Result = await SendCommandAsync(Method, Params);
        SessionId = SavedSession;
        return Result;
    }

    internal async Task<List<JsonNode>> GetPageTargetsAsync()
    {
        var Targets = await SendBrowserCommandAsync(Cdp.TargetGetTargets);
        return
        [
            .. Targets![CdpKey.TargetInfos]!.AsArray()
                .Where(T => T![CdpKey.Type]!.ToString() == CdpKey.Page
                    && (!T![CdpKey.Url]!.ToString().StartsWith(CdpProto.ChromeScheme, StringComparison.Ordinal) || T![CdpKey.Url]!.ToString() == CdpProto.NewTabUrl)
                    && !T![CdpKey.Url]!.ToString().StartsWith(CdpProto.ChromeExtensionScheme, StringComparison.Ordinal))
                .Select(T => T!),
        ];
    }

    internal async Task AttachToTargetAsync(string TargetId)
    {
        var Session = await SendBrowserCommandAsync(Cdp.TargetAttachToTarget, new JsonObject { [CdpKey.TargetId] = TargetId, [CdpKey.Flatten] = true });
        SessionId = Session![CdpKey.SessionId]!.ToString();
        await SendCommandAsync(Cdp.PageEnable);
        await SendCommandAsync(Cdp.RuntimeEnable);
        await SendCommandAsync(Cdp.DomEnable);
        await SendCommandAsync(Cdp.NetworkEnable);
    }

    internal async Task EnsurePageAttachedAsync()
    {
        if (SessionId != null)
        {
            return;
        }

        var Pages = await GetPageTargetsAsync();
        if (Pages.Count == 0)
        {
            Console.Error.WriteLine("No pages open");
            Environment.Exit(1);
        }

        await AttachToTargetAsync(Pages[0][CdpKey.TargetId]!.ToString());
    }

    internal async Task<string> EvaluateExpressionAsync(string Expression, bool AwaitPromise = false)
    {
        var Params = new JsonObject { [CdpKey.Expression] = Expression, [CdpKey.ReturnByValue] = true };
        if (AwaitPromise)
        {
            Params[CdpKey.AwaitPromise] = true;
        }

        var Result = await SendCommandAsync(Cdp.RuntimeEvaluate, Params);
        return Result?[CdpKey.ExceptionDetails] != null
            ? $"{CdpProto.ErrorPrefix}{Result[CdpKey.ExceptionDetails]![CdpKey.Text]}"
            : Result?[CdpKey.Result]?[CdpKey.Value]?.ToString() ?? string.Empty;
    }

    private static string BuildUidSelector(string Uid) => $"{CdpProto.DataUidSelector}{Uid}{CdpProto.DataUidSelectorEnd}";
}
}
