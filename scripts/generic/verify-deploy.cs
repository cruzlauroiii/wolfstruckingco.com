#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// verify-deploy.cs - Specific. Owns the live-deploy probe ARGS (URLs +
// grep patterns + labels). Builds a JSONL batch and shells out to the
// generic fetch-url.cs --batch in a single SDK invocation (item #23/#24).
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scripts;

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

(string Label, string Path, string Mode, string Pattern)[] Probes =
[
    ("home: header has Sign In, not Sign out sam", "/", "grep", "Sign In|Sign out sam"),
    ("home: cards no longer hardcode #fff5e6/#f3f6fa/#eef2ff", "/", "grep", "fff5e6|f3f6fa;|eef2ff"),
    ("home: 12 emoji CTAs survive", "/", "grep", "📦|🚛|📍|🤖|🧭|📞|📎|📊|🚀|🔓|📝"),
    ("Login: SSO buttons via @onclick", "/Login/", "grep", "SsoBtn|prtask"),
    ("Login: burger uses MenuToggle", "/Login/", "grep", "MenuToggle|BurgerBtn"),
    ("Marketplace: 🛒 emoji in title", "/Marketplace/", "grep", "🛒 Marketplace"),
    ("Marketplace: Photo by overlay shows role label", "/Marketplace/", "grep", "Photo by [A-Z][a-z]+"),
    ("Sell/Chat: empty for unauth (no BYD seed)", "/Sell/Chat/", "grep", "BYD|2024 BYD Han EV"),
    ("Sell/Chat: auth-gate sign-in prompt", "/Sell/Chat/", "grep", "Please.*sign in|AuthWarn"),
    ("Applicant: auth-gate sign-in prompt", "/Applicant/", "grep", "Please.*sign in|AuthWarn"),
    ("Dispatcher: auth-gate sign-in prompt", "/Dispatcher/", "grep", "Please.*sign in|AuthWarn"),
    ("wwwroot/app: no wolfs-interop.js script tag", "/app/", "grep", "wolfs-interop.js|voice-bridge.js"),
    ("Theme chip: uses themeWrite/themeCycle", "/", "grep", "wt-theme-chip|themeWrite"),
    ("Stage padding for video framing", "/Login/", "grep", "padding:14vh|padding:10vh"),
];

var Tmp = Path.Combine(Path.GetTempPath(), $"wolfs-verify-{Guid.NewGuid():N}.jsonl");
var Opts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
await using (var Sw = new StreamWriter(Tmp))
{
    foreach (var (Label, Path2, Mode, Pattern) in Probes)
    {
        var Obj = new JsonObject
        {
            ["url"] = BaseUrl + Path2,
            ["mode"] = Mode,
            ["pattern"] = Pattern,
            ["label"] = Label,
            ["after"] = 0,
            ["before"] = 0,
        };
        await Sw.WriteLineAsync(Obj.ToJsonString(Opts));
    }
}
await Console.Out.WriteLineAsync($"wrote batch: {Tmp} ({Probes.Length} probes)");
var Psi = new ProcessStartInfo("dotnet", $"run scripts/fetch-url.cs -- --batch \"{Tmp}\"") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = Paths.Repo };
using var Proc = Process.Start(Psi)!;
await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
await Proc.WaitForExitAsync();
try { File.Delete(Tmp); } catch (IOException) { }
return Proc.ExitCode;
