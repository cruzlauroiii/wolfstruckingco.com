#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// patch-home-emojis.cs - Specific. Bumps emoji density on the home page CTAs +
// feature cards per #3. Owns ALL the (path, find, replace, idempotent) inputs
// and shells out to the GENERIC patch-file.cs --batch in a single invocation.
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scripts;

const string Home = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\HomePage.razor";

(string Find, string Replace)[] Patches =
[
    (">Start a shipment</a>", ">📦 Start a shipment</a>"),
    (">Apply to drive</a>", ">🚛 Apply to drive</a>"),
    (">Track</a>", ">📍 Track</a>"),
    ("<h2>Agent-composed jobs</h2>", "<h2>🤖 Agent-composed jobs</h2>"),
    ("<h2>Voice navigation</h2>", "<h2>🧭 Voice navigation</h2>"),
    ("<h2>Real-time tracking</h2>", "<h2>📍 Real-time tracking</h2>"),
    ("<h2>Dispatcher you can call</h2>", "<h2>📞 Dispatcher you can call</h2>"),
    ("<h2>Inline credential intake</h2>", "<h2>📎 Inline credential intake</h2>"),
    ("<h2>Live numbers</h2>", "<h2>📊 Live numbers</h2>"),
    ("<h2>Get started</h2>", "<h2>🚀 Get started</h2>"),
    (">Sign in</a>", ">🔓 Sign in</a>"),
    (">Create account</a>", ">📝 Create account</a>"),
];

var Tmp = Path.Combine(Path.GetTempPath(), $"wolfs-home-emoji-{Guid.NewGuid():N}.jsonl");
var Opts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
await using (var Sw = new StreamWriter(Tmp))
{
    foreach (var (Find, Replace) in Patches)
    {
        var Obj = new JsonObject
        {
            ["path"] = Home,
            ["find"] = Find,
            ["replace"] = Replace,
            ["idempotent"] = true,
        };
        await Sw.WriteLineAsync(Obj.ToJsonString(Opts));
    }
}
await Console.Out.WriteLineAsync($"wrote batch: {Tmp} ({Patches.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} patches)");
var Psi = new ProcessStartInfo("dotnet", $"run scripts/patch-file.cs -- --batch \"{Tmp}\"")
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    WorkingDirectory = Paths.Repo,
};
using var Proc = Process.Start(Psi)!;
await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
await Proc.WaitForExitAsync();
try { File.Delete(Tmp); } catch (IOException) { }
return Proc.ExitCode;
