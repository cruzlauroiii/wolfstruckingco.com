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
            if (Lines.Length >= 2 && int.TryParse(Lines[0].Trim(), out var ActivePort) && await PortAcceptsTcpAsync(ActivePort))
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

    private static async Task<bool> PortRespondsAsync(int Port)
    {
        try
        {
            using var Http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var Json = await Http.GetStringAsync($"http://127.0.0.1:{Port}/json/version");
            return Json.Contains("webSocketDebuggerUrl", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> PortAcceptsTcpAsync(int Port)
    {
        try
        {
            using var Client = new System.Net.Sockets.TcpClient();
            using var Timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await Client.ConnectAsync("127.0.0.1", Port, Timeout.Token);
            return true;
        }
        catch
        {
            return false;
        }
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
