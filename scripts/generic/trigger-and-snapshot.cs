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
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

const string Repo = @"C:\repo\public\wolfstruckingco.com\main";

static async Task Subprocess(string ScriptPath, string ExtraArgs)
{
    var Psi = new ProcessStartInfo("dotnet", $"run \"{ScriptPath}\" {ExtraArgs}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = @"C:\repo\public\wolfstruckingco.com\main",
    };
    using var P = Process.Start(Psi)!;
    _ = await P.StandardOutput.ReadToEndAsync();
    _ = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
}

await Subprocess($@"{Repo}\scripts\chrome-devtools.cs", "-- focus_chrome");

var ChromeUserData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
var Lines = await File.ReadAllLinesAsync(Path.Combine(ChromeUserData, "DevToolsActivePort"));
var Port = int.Parse(Lines[0].Trim());
var WsPath = Lines[1].Trim();
var Url = $"ws://127.0.0.1:{Port}{WsPath}";

using var Ws = new ClientWebSocket();
Ws.Options.SetRequestHeader("Origin", "http://localhost");
var ConnectTask = Ws.ConnectAsync(new Uri(Url), default);
await Task.Delay(800);
await Subprocess($@"{Repo}\scripts\chrome-devtools.cs", "-- screenshot_desktop --path C:\\Users\\user1\\AppData\\Local\\Temp\\infobar-during.png");
await Subprocess($@"{Repo}\scripts\chrome-devtools.cs", "-- allow");
await Task.Delay(200);
await Subprocess($@"{Repo}\scripts\chrome-devtools.cs", "-- screenshot_desktop --path C:\\Users\\user1\\AppData\\Local\\Temp\\infobar-after.png");
await ConnectTask;
Console.WriteLine($"WS state: {Ws.State}");
return 0;
