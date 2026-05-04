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

        using var Timeout = new CancellationTokenSource(CdpTimeout.ConnectTimeoutMs);
        await WebSocket!.SendAsync(Encoding.UTF8.GetBytes(Message.ToJsonString()), WebSocketMessageType.Text, true, Timeout.Token);
        var Buffer = new byte[CdpTimeout.BufferSize];
        var Builder = new StringBuilder();
        while (true)
        {
            var Result = await WebSocket.ReceiveAsync(Buffer, Timeout.Token);
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
