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

var RunTask = new CdpCli().RunAsync(args);
if (await Task.WhenAny(RunTask, Task.Delay(CdpTimeout.ProcessWaitMs)) != RunTask)
{
    Console.Error.WriteLine("chrome-devtools timed out");
    return 124;
}
await RunTask;
return 0;

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
            var Body = await File.ReadAllTextAsync(Argv[0]);
            var OutMatch = System.Text.RegularExpressions.Regex.Match(Body, "const\\s+string\\s+OutputPath\\s*=\\s*@?\"(?<v>[^\"]*)\"");
            if (OutMatch.Success) { OutputPath = OutMatch.Groups["v"].Value; }
            SilentMode = !string.IsNullOrEmpty(OutputPath);
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
            else
            {
                Console.Write(Captured);
            }
        }
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
