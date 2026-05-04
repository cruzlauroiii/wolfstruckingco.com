using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using File = System.IO.File;
using Path = System.IO.Path;

namespace CdpTool;

public sealed partial class CdpCli : IDisposable
{
    private static readonly JsonSerializerOptions JsonIndented = new(JsonSerializerDefaults.General) { WriteIndented = true, TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() };

    public void Dispose()
    {
        WebSocket?.Dispose();
        GC.SuppressFinalize(this);
    }

    internal async Task ExecuteListPagesAsync()
    {
        var Pages = await GetPageTargetsAsync();
        Console.WriteLine("## Pages");
        for (var Index = 0; Index < Pages.Count; Index++)
        {
            var Url = Pages[Index][CdpKey.Url]!.ToString();
            var Title = Pages[Index][CdpKey.Title]?.ToString() ?? string.Empty;
            Console.WriteLine(string.Concat(Index + 1, CdpMsg.ColonSpace, Url, Title.Length > 0 ? string.Concat(CdpMsg.ParenOpen, Title, CdpMsg.ParenClose) : string.Empty));
        }
    }

    internal async Task ExecuteSelectPageAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpArg.PageId, out var PageId)) { Console.Error.WriteLine("Required: pageId"); return; }
        var Pages = await GetPageTargetsAsync();
        var Index = int.Parse(PageId.ToString()!, System.Globalization.CultureInfo.InvariantCulture) - 1;
        if (Index < 0 || Index >= Pages.Count) { Console.Error.WriteLine(string.Concat(CdpMsg.InvalidPageIdRange, Pages.Count)); return; }
        await AttachToTargetAsync(Pages[Index][CdpKey.TargetId]!.ToString());
        Console.WriteLine(string.Concat(CdpMsg.SelectedPage, int.Parse(PageId.ToString()!, System.Globalization.CultureInfo.InvariantCulture), CdpMsg.ColonSpace, Pages[Index][CdpKey.Url]));
    }

    internal async Task ExecuteClosePageAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpArg.PageId, out var PageId)) { Console.Error.WriteLine("Required: pageId"); return; }
        var Pages = await GetPageTargetsAsync();
        var Index = int.Parse(PageId.ToString()!, System.Globalization.CultureInfo.InvariantCulture) - 1;
        if (Index < 0 || Index >= Pages.Count) { Console.Error.WriteLine("Invalid pageId"); return; }
        await SendBrowserCommandAsync(Cdp.TargetCloseTarget, new JsonObject { [CdpKey.TargetId] = Pages[Index][CdpKey.TargetId]!.ToString() });
        Console.WriteLine(string.Concat(CdpMsg.ClosedPage, int.Parse(PageId.ToString()!, System.Globalization.CultureInfo.InvariantCulture)));
    }

    internal async Task ExecuteNewPageAsync(Dictionary<string, object> Args)
    {
        var Url = Args.TryGetValue(CdpKey.Url, out var UrlValue) ? UrlValue.ToString()! : CdpProto.AboutBlank;
        var Target = await SendBrowserCommandAsync(Cdp.TargetCreateTarget, new JsonObject { [CdpKey.Url] = CdpProto.AboutBlank });
        await SendBrowserCommandAsync(Cdp.TargetActivateTarget, new JsonObject { [CdpKey.TargetId] = Target![CdpKey.TargetId]!.ToString() });
        await AttachToTargetAsync(Target![CdpKey.TargetId]!.ToString());
        await SendCommandAsync(Cdp.PageNavigate, new JsonObject { [CdpKey.Url] = Url });
        await Task.Delay(CdpTimeout.PageLoadDelayMs);
        Console.WriteLine(string.Concat(CdpMsg.Opened, Url));
        await ExecuteListPagesAsync();
    }

    internal async Task ExecuteNavigatePageAsync(Dictionary<string, object> Args)
    {
        await EnsurePageAttachedAsync();
        if (Args.TryGetValue(CdpKey.Type, out var NavType))
        {
            switch (NavType.ToString())
            {
                case "back": await EvaluateExpressionAsync(CdpJs.HistoryBack); await Task.Delay(CdpTimeout.PageLoadDelayMs); break;
                case "forward": await EvaluateExpressionAsync(CdpJs.HistoryForward); await Task.Delay(CdpTimeout.PageLoadDelayMs); break;
                case "reload":
                    var IgnoreCache = Args.ContainsKey(CdpKey.IgnoreCache) && bool.Parse(Args[CdpKey.IgnoreCache].ToString()!);
                    await SendCommandAsync(Cdp.PageReload, new JsonObject { [CdpKey.IgnoreCache] = IgnoreCache });
                    await Task.Delay(CdpTimeout.PageLoadDelayMs); break;
                case "url":
                    if (!Args.TryGetValue(CdpKey.Url, out var NavUrl)) { Console.Error.WriteLine("Required: url"); return; }
                    await SendCommandAsync(Cdp.PageNavigate, new JsonObject { [CdpKey.Url] = NavUrl.ToString()! });
                    await Task.Delay(CdpTimeout.NavigationDelayMs);
                    Console.WriteLine(string.Concat(CdpMsg.NavigatedTo, NavUrl)); break;
            }
        }
        else if (Args.TryGetValue(CdpKey.Url, out var DirectUrl))
        {
            await SendCommandAsync(Cdp.PageNavigate, new JsonObject { [CdpKey.Url] = DirectUrl.ToString()! });
            await Task.Delay(CdpTimeout.NavigationDelayMs);
            Console.WriteLine(string.Concat(CdpMsg.NavigatedTo, DirectUrl));
        }
        await ExecuteListPagesAsync();
    }

    internal async Task ExecuteTakeScreenshotAsync(Dictionary<string, object> Args)
    {
        await EnsurePageAttachedAsync();
        var Mobile = Args.TryGetValue("mobile", out var MobileArg) && (MobileArg is bool MB ? MB : string.Equals(MobileArg?.ToString(), "true", StringComparison.OrdinalIgnoreCase));
        var DsfArg2 = Args.TryGetValue("dsf", out var DsfA) && int.TryParse(DsfA?.ToString(), out var DsfP) ? DsfP : 1;
        if (Mobile && Args.TryGetValue(CdpKey.Width, out var EmuW) && Args.TryGetValue(CdpKey.Height, out var EmuH))
        {
            await SendCommandAsync(Cdp.EmulationSetDeviceMetrics, new JsonObject { [CdpKey.Width] = int.Parse(EmuW.ToString()!, System.Globalization.CultureInfo.InvariantCulture), [CdpKey.Height] = int.Parse(EmuH.ToString()!, System.Globalization.CultureInfo.InvariantCulture), [CdpKey.DeviceScaleFactor] = DsfArg2, [CdpKey.Mobile] = true });
            await SendCommandAsync("Runtime.evaluate", new JsonObject { ["expression"] = "window.dispatchEvent(new Event('resize'));" });
            await Task.Delay(800);
        }
        var Format = Args.TryGetValue(CdpKey.Format, out var FormatValue) ? FormatValue.ToString()! : CdpProto.PngFormat;
        var ScreenshotParams = new JsonObject { [CdpKey.Format] = Format };
        if (Args.TryGetValue(CdpKey.Quality, out var Quality)) { ScreenshotParams[CdpKey.Quality] = int.Parse(Quality.ToString()!, System.Globalization.CultureInfo.InvariantCulture); }
        if (Args.TryGetValue(CdpArg.FullPage, out var FullPageVal) && bool.Parse(FullPageVal.ToString()!))
        {
            var Metrics = await SendCommandAsync(Cdp.PageGetLayoutMetrics);
            ScreenshotParams[CdpKey.Clip] = new JsonObject { [CdpKey.X] = 0, [CdpKey.Y] = 0, [CdpKey.Width] = Metrics![CdpKey.ContentSize]![CdpKey.Width]!.GetValue<double>(), [CdpKey.Height] = Metrics![CdpKey.ContentSize]![CdpKey.Height]!.GetValue<double>(), [CdpKey.Scale] = 1 };
        }
        else if (Args.TryGetValue(CdpKey.Width, out var ClipW) && Args.TryGetValue(CdpKey.Height, out var ClipH))
        {
            var ScaleVal = Args.TryGetValue("scale", out var ScaleArg) && double.TryParse(ScaleArg?.ToString(), System.Globalization.CultureInfo.InvariantCulture, out var ScaleParsed) ? ScaleParsed : 1.0;
            ScreenshotParams[CdpKey.Clip] = new JsonObject { [CdpKey.X] = 0, [CdpKey.Y] = 0, [CdpKey.Width] = double.Parse(ClipW.ToString()!, System.Globalization.CultureInfo.InvariantCulture), [CdpKey.Height] = double.Parse(ClipH.ToString()!, System.Globalization.CultureInfo.InvariantCulture), [CdpKey.Scale] = ScaleVal };
        }
        var ScreenshotResult = await SendCommandAsync(Cdp.PageCaptureScreenshot, ScreenshotParams);
        var ImageData = Convert.FromBase64String(ScreenshotResult![CdpKey.Data]!.ToString());
        var OutputPath = Args.TryGetValue(CdpArg.FilePath, out var FilePath) ? FilePath.ToString()! : Path.Combine(Path.GetTempPath(), $"{CdpProto.ScreenshotPrefix}{Guid.NewGuid():N}.{Format}");
        File.WriteAllBytes(OutputPath, ImageData);
        Console.WriteLine(string.Concat(CdpMsg.ScreenshotSaved, OutputPath));
    }

    internal async Task ExecuteTakeSnapshotAsync(Dictionary<string, object> Args)
    {
        await EnsurePageAttachedAsync();
        var Tree = await SendCommandAsync(Cdp.AccessibilityGetFullAxTree);
        var Nodes = Tree![CdpKey.Nodes]!.AsArray();
        var Output = new StringBuilder();
        Output.AppendLine("## Accessibility Snapshot");
        foreach (var Node in Nodes)
        {
            var Role = Node![CdpKey.Role]?[CdpKey.Value]?.ToString() ?? string.Empty;
            if (Role.Length == 0 || Role is "none" or "InlineTextBox") { continue; }
            var Name = Node[CdpKey.Name]?[CdpKey.Value]?.ToString() ?? string.Empty;
            var NodeIdVal = Node[CdpKey.NodeId]?.ToString() ?? string.Empty;
            Output.AppendLine(string.Concat(CdpMsg.BracketOpen, NodeIdVal, CdpMsg.BracketClose, Role, Name.Length > 0 ? string.Concat(CdpMsg.QuoteOpen, Name, CdpMsg.QuoteClose) : string.Empty));
        }
        var Content = Output.ToString();
        if (Args.TryGetValue(CdpArg.FilePath, out var FilePath)) { File.WriteAllText(FilePath.ToString()!, Content); Console.WriteLine(string.Concat(CdpMsg.SnapshotSaved, FilePath)); }
        else { Console.Write(Content); }
    }

    internal async Task ExecuteEvaluateScriptAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpArg.Function, out var Function)) { Console.Error.WriteLine("Required: function"); return; }
        await EnsurePageAttachedAsync();
        var Result = await SendCommandAsync(Cdp.RuntimeEvaluate, new JsonObject { [CdpKey.Expression] = $"({Function}{CdpMsg.InvokeWrapper}", [CdpKey.ReturnByValue] = true, [CdpKey.AwaitPromise] = true });
        if (Result?[CdpKey.ExceptionDetails] != null) { Console.Error.WriteLine(string.Concat(CdpMsg.ErrorLabel, Result[CdpKey.ExceptionDetails]![CdpKey.Text])); }
        else { Console.WriteLine(Result?[CdpKey.Result]?[CdpKey.Value] != null ? JsonSerializer.Serialize(Result[CdpKey.Result]![CdpKey.Value], JsonIndented) : CdpEscape.Undefined); }
    }

    internal async Task ExecuteClickAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpArg.Uid, out var Uid)) { Console.Error.WriteLine("Required: uid"); return; }
        await EnsurePageAttachedAsync();
        var DoubleClick = Args.ContainsKey(CdpArg.DblClick) && bool.Parse(Args[CdpArg.DblClick].ToString()!);
        var Escaped = Uid.ToString()!.Replace(CdpEscape.SingleQuote, CdpEscape.SingleQuoteEscaped);
        var Sel = BuildUidSelector(Escaped);
        Console.WriteLine(await EvaluateExpressionAsync(string.Format(System.Globalization.CultureInfo.InvariantCulture, CdpJs.ClickScript, Sel, Escaped, DoubleClick ? CdpJs.ClickAgain : string.Empty), true));
    }

    internal async Task ExecuteHoverAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpArg.Uid, out var Uid)) { Console.Error.WriteLine("Required: uid"); return; }
        await EnsurePageAttachedAsync();
        Console.WriteLine(await EvaluateExpressionAsync(string.Format(System.Globalization.CultureInfo.InvariantCulture, CdpJs.HoverScript, BuildUidSelector(Uid.ToString()!))));
    }

    internal async Task ExecuteFillAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpArg.Uid, out var Uid) || !Args.TryGetValue(CdpKey.Value, out var Value)) { Console.Error.WriteLine("Required: uid, value"); return; }
        await EnsurePageAttachedAsync();
        var Escaped = Value.ToString()!.Replace(CdpEscape.Backslash, CdpEscape.BackslashEscaped).Replace(CdpEscape.SingleQuote, CdpEscape.SingleQuoteEscaped);
        Console.WriteLine(await EvaluateExpressionAsync(string.Format(System.Globalization.CultureInfo.InvariantCulture, CdpJs.FillScript, BuildUidSelector(Uid.ToString()!), Escaped)));
    }

    internal async Task ExecuteTypeTextAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpKey.Text, out var Text)) { Console.Error.WriteLine("Required: text"); return; }
        await EnsurePageAttachedAsync();
        foreach (var Character in Text.ToString()!) { await SendCommandAsync(Cdp.InputDispatchKeyEvent, new JsonObject { [CdpKey.Type] = CdpProto.KeyChar, [CdpKey.Text] = Character.ToString() }); }
        if (Args.TryGetValue(CdpArg.SubmitKey, out var SubmitKey)) { await SendCommandAsync(Cdp.InputDispatchKeyEvent, new JsonObject { [CdpKey.Type] = CdpProto.KeyDown, [CdpKey.Key] = SubmitKey.ToString() }); }
        Console.WriteLine(string.Concat(CdpMsg.Typed, Text));
    }

    internal async Task ExecutePressKeyAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpKey.Key, out var Key)) { Console.Error.WriteLine("Required: key"); return; }
        await EnsurePageAttachedAsync();
        var Parts = Key.ToString()!.Split('+');
        var Mods = 0;
        foreach (var Modifier in Parts.SkipLast(1)) { switch (Modifier.ToLower(System.Globalization.CultureInfo.InvariantCulture)) { case "control" or "ctrl": Mods |= CdpModifier.Control; break; case "alt": Mods |= CdpModifier.Alt; break; case "shift": Mods |= CdpModifier.Shift; break; case "meta" or "cmd": Mods |= CdpModifier.Meta; break; } }
        await SendCommandAsync(Cdp.InputDispatchKeyEvent, new JsonObject { [CdpKey.Type] = CdpProto.KeyDown, [CdpKey.Key] = Parts[^1], [CdpKey.Modifiers] = Mods });
        await SendCommandAsync(Cdp.InputDispatchKeyEvent, new JsonObject { [CdpKey.Type] = CdpProto.KeyUp, [CdpKey.Key] = Parts[^1], [CdpKey.Modifiers] = Mods });
        Console.WriteLine(string.Concat(CdpMsg.Pressed, Key));
    }

    internal async Task ExecuteListConsoleMessagesAsync()
    {
        await EnsurePageAttachedAsync();
        var Messages = EventBuffer.Where(E => E[CdpKey.Method]?.ToString() is Cdp.RuntimeConsoleApiCalled or Cdp.RuntimeExceptionThrown).ToList();
        if (Messages.Count == 0) { Console.WriteLine("No console messages captured."); return; }
        Console.WriteLine("## Console Messages");
        foreach (var Msg in Messages)
        {
            if (Msg[CdpKey.Method]!.ToString() == Cdp.RuntimeConsoleApiCalled)
            {
                var MsgType = Msg[CdpKey.Params]![CdpKey.Type]?.ToString() ?? "log";
                var Content = string.Join(" ", Msg[CdpKey.Params]![CdpKey.Args]!.AsArray().Select(A => A![CdpKey.Value]?.ToString() ?? A![CdpKey.Description]?.ToString() ?? string.Empty));
                Console.WriteLine($"{CdpMsg.SquareOpen}{MsgType}{CdpMsg.BracketClose}{Content}");
            }
            else
            {
                Console.WriteLine($"{CdpMsg.ErrorLog}{Msg[CdpKey.Params]![CdpKey.ExceptionDetails]?[CdpKey.Text]}");
            }
        }
    }

    internal async Task ExecuteListNetworkRequestsAsync()
    {
        await EnsurePageAttachedAsync();
        var Requests = EventBuffer.Where(E => E[CdpKey.Method]?.ToString() == Cdp.NetworkRequestWillBeSent).ToList();
        if (Requests.Count == 0) { Console.WriteLine("No network requests captured."); return; }
        Console.WriteLine("## Network Requests");
        var Counter = 1;
        foreach (var Request in Requests.TakeLast(50))
        {
            Console.WriteLine($"{Counter++}{CdpMsg.DotSpace}{Request[CdpKey.Params]![CdpKey.Request]![CdpKey.Method]} {Request[CdpKey.Params]![CdpKey.Request]![CdpKey.Url]}");
        }
    }

    internal async Task ExecuteResizePageAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpKey.Width, out var Width) || !Args.TryGetValue(CdpKey.Height, out var Height)) { Console.Error.WriteLine("Required: width, height"); return; }
        await EnsurePageAttachedAsync();
        var Mobile = Args.TryGetValue("mobile", out var MobileArg) && (MobileArg is bool MB ? MB : string.Equals(MobileArg?.ToString(), "true", StringComparison.OrdinalIgnoreCase));
        var Dsf = Args.TryGetValue("dsf", out var DsfArg) && int.TryParse(DsfArg?.ToString(), out var DsfParsed) ? DsfParsed : 1;
        await SendCommandAsync(Cdp.EmulationSetDeviceMetrics, new JsonObject { [CdpKey.Width] = int.Parse(Width.ToString()!, System.Globalization.CultureInfo.InvariantCulture), [CdpKey.Height] = int.Parse(Height.ToString()!, System.Globalization.CultureInfo.InvariantCulture), [CdpKey.DeviceScaleFactor] = Dsf, [CdpKey.Mobile] = Mobile });
        var MobileSuffix = Mobile ? " (mobile)" : string.Empty;
        var DsfSuffix = Dsf > 1 ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $" dsf={Dsf}") : string.Empty;
        Console.WriteLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{CdpMsg.ResizedTo}{Width}{CdpMsg.Separator}{Height}{MobileSuffix}{DsfSuffix}"));
    }

    internal async Task ExecuteEmulateAsync(Dictionary<string, object> Args)
    {
        await EnsurePageAttachedAsync();
        if (Args.TryGetValue(CdpKey.UserAgent, out var UserAgent))
        {
            await SendCommandAsync(Cdp.EmulationSetUserAgent, new JsonObject { [CdpKey.UserAgent] = UserAgent.ToString()! });
        }
        if (Args.TryGetValue(CdpArg.Geolocation, out var GeoLocation))
        {
            var Coordinates = GeoLocation.ToString()!.Split('x');
            if (Coordinates.Length == 2)
            {
                await SendCommandAsync(Cdp.EmulationSetGeolocation, new JsonObject { [CdpKey.Latitude] = double.Parse(Coordinates[0], System.Globalization.CultureInfo.InvariantCulture), [CdpKey.Longitude] = double.Parse(Coordinates[1], System.Globalization.CultureInfo.InvariantCulture), [CdpKey.Accuracy] = 1 });
            }
        }
        if (Args.TryGetValue(CdpArg.ColorScheme, out var ColorScheme))
        {
            await SendCommandAsync(Cdp.EmulationSetMedia, new JsonObject { [CdpKey.Features] = new JsonArray { new JsonObject { [CdpKey.Name] = CdpProto.PrefersColorScheme, [CdpKey.Value] = ColorScheme.ToString()! } } });
        }
        Console.WriteLine("Emulation applied");
    }

    internal async Task ExecuteHandleDialogAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpArg.Action, out var Action)) { Console.Error.WriteLine("Required: action (accept|dismiss)"); return; }
        var DialogParams = new JsonObject { [CdpKey.Accept] = Action.ToString() == "accept" };
        if (Args.TryGetValue(CdpKey.PromptText, out var PromptTextVal)) { DialogParams[CdpKey.PromptText] = PromptTextVal.ToString(); }
        await SendCommandAsync(Cdp.PageHandleJavaScriptDialog, DialogParams);
        Console.WriteLine(string.Concat(CdpMsg.DialogPrefix, Action, CdpMsg.DialogSuffix));
    }

    internal async Task ExecuteDragAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpArg.FromUid, out var FromUid) || !Args.TryGetValue(CdpArg.ToUid, out var ToUid)) { Console.Error.WriteLine("Required: from_uid, to_uid"); return; }
        await EnsurePageAttachedAsync();
        Console.WriteLine(await EvaluateExpressionAsync(string.Format(System.Globalization.CultureInfo.InvariantCulture, CdpJs.DragScript, BuildUidSelector(FromUid.ToString()!), BuildUidSelector(ToUid.ToString()!))));
    }

    internal async Task ExecuteUploadFileAsync(Dictionary<string, object> Args)
    {
        if (!Args.TryGetValue(CdpArg.Uid, out var Uid) || !Args.TryGetValue(CdpArg.FilePath, out var FilePath)) { Console.Error.WriteLine("Required: uid, filePath"); return; }
        await EnsurePageAttachedAsync();
        var Document = await SendCommandAsync(Cdp.DomGetDocument);
        var Found = await SendCommandAsync(Cdp.DomQuerySelector, new JsonObject { [CdpKey.NodeId] = Document![CdpKey.Root]![CdpKey.NodeId]!.GetValue<int>(), [CdpKey.Selector] = BuildUidSelector(Uid.ToString()!) });
        if (Found?[CdpKey.NodeId]?.GetValue<int>() is > 0)
        {
            await SendCommandAsync(Cdp.DomSetFileInputFiles, new JsonObject { [CdpKey.NodeId] = Found[CdpKey.NodeId]!.GetValue<int>(), [CdpKey.Files] = new JsonArray { JsonValue.Create(Path.GetFullPath(FilePath.ToString()!)) } });
            Console.WriteLine(string.Concat(CdpMsg.Uploaded, FilePath));
        }
        else { Console.Error.WriteLine("File input not found"); }
    }
}
