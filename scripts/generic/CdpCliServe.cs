using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using File = System.IO.File;
using Path = System.IO.Path;

namespace CdpTool;

public sealed partial class CdpCli
{
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
            case "scene_one_screenshot":
                await ExecuteSceneOneScreenshotAsync(ParsedArgs);
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
}
