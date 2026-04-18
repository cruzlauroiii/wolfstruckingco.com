#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// patch-page-emojis-batch.cs - Specific. Owns the (file, find, replace)
// emoji-bump tuples for every remaining page (#3). Builds a JSONL batch and
// shells out to the GENERIC patch-file.cs --batch in ONE invocation.
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scripts;

const string Pages = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\";

(string File, string Find, string Replace)[] Patches =
[
    (Pages + "AboutPage.razor", "<h2>Freight marketplace</h2>", "<h2>🛒 Freight marketplace</h2>"),
    (Pages + "AboutPage.razor", "<h2>Real-time tracking</h2>", "<h2>📍 Real-time tracking</h2>"),
    (Pages + "AboutPage.razor", "<h2>Voice navigation</h2>", "<h2>🧭 Voice navigation</h2>"),
    (Pages + "AboutPage.razor", "<h2>Dispatcher you can call</h2>", "<h2>📞 Dispatcher you can call</h2>"),
    (Pages + "PricingPage.razor", "<h2>Base rate</h2>", "<h2>💵 Base rate</h2>"),
    (Pages + "PricingPage.razor", "<h2>Multi-stop</h2>", "<h2>🛣️ Multi-stop</h2>"),
    (Pages + "PricingPage.razor", "<h2>Rush delivery</h2>", "<h2>⚡ Rush delivery</h2>"),
    (Pages + "PricingPage.razor", "<h2>Heavy freight</h2>", "<h2>🏗️ Heavy freight</h2>"),
    (Pages + "ApplyPage.razor", ">Want to drive for Wolfs?</h1>", ">🚛 Want to drive for Wolfs?</h1>"),
    (Pages + "ApplyPage.razor", ">What you need to apply</h2>", ">📋 What you need to apply</h2>"),
    (Pages + "ApplyPage.razor", ">Start application — chat with agent</a>", ">💬 Start application — chat with agent</a>"),
    (Pages + "ApplyPage.razor", ">Application sent</h1>", ">✅ Application sent</h1>"),
    (Pages + "ApplyPage.razor", ">View my chat</a>", ">💬 View my chat</a>"),
    (Pages + "ApplyPage.razor", ">View my documents</a>", ">📎 View my documents</a>"),
    (Pages + "ApplyPage.razor", ">Go to driver home</a>", ">🏠 Go to driver home</a>"),
    (Pages + "TrackPage.razor", ">Track your delivery</h1>", ">📍 Track your delivery</h1>"),
    (Pages + "DashboardPage.razor", ">Accept job</a>", ">✅ Accept job</a>"),
    (Pages + "DashboardPage.razor", ">Preview route</a>", ">🗺️ Preview route</a>"),
    (Pages + "AdminPage.razor", ">Admin home</h1>", ">🛡️ Admin home</h1>"),
    (Pages + "AdminPage.razor", ">Open KPI dashboard</a>", ">📊 Open KPI dashboard</a>"),
    (Pages + "AdminPage.razor", ">Open dispatcher console</a>", ">📞 Open dispatcher console</a>"),
    (Pages + "HiringHallPage.razor", ">Hiring Hall</h1>", ">🧑 Hiring Hall</h1>"),
    (Pages + "HiringHallPage.razor", ">Approve all · assign badges + roles</button>", ">✅ Approve all · assign badges + roles</button>"),
    (Pages + "HiringHallPage.razor", ">Review one by one</button>", ">👀 Review one by one</button>"),
    (Pages + "CareersPage.razor", ">Why drivers choose us</h2>", ">⭐ Why drivers choose us</h2>"),
    (Pages + "CareersPage.razor", ">Open positions</h2>", ">📋 Open positions</h2>"),
    (Pages + "SignUpPage.razor", ">Sign up</h1>", ">📝 Sign up</h1>"),
    (Pages + "SignUpPage.razor", ">Create account</button>", ">🚀 Create account</button>"),
    (Pages + "SignUpPage.razor", ">I already have an account</a>", ">🔓 I already have an account</a>"),
];

var Tmp = Path.Combine(Path.GetTempPath(), $"wolfs-page-emoji-{Guid.NewGuid():N}.jsonl");
var Opts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
await using (var Sw = new StreamWriter(Tmp))
{
    foreach (var (File2, Find, Replace) in Patches)
    {
        var Obj = new JsonObject
        {
            ["path"] = File2,
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
