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
#:include CdpCliServe.cs
#:include CdpCliConnection.cs
#:include CdpCliTransport.cs
#:include CdpCliSceneOne.cs
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

}
}
