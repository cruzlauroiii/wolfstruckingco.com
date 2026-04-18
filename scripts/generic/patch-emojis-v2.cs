#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// patch-emojis-v2.cs - Specific. Demonstrates the generic↔specific delegation
// pattern (item #23): builds a list of {path, find, replace, idempotent}
// patches and shells out to the GENERIC patch-file.cs --batch in a SINGLE
// invocation. Per-target file I/O lives in patch-file.cs, not here.
//
// Run: dotnet run scripts/patch-emojis-v2.cs
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scripts;

const string Pages = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\";

(string Path, string Find, string Replace)[] Patches =
[
    (Pages + "MarketplacePage.razor", "<h1>Marketplace</h1>", "<h1>🛒 Marketplace</h1>"),
    (Pages + "MarketplacePage.razor", "<h2>Post a listing</h2>", "<h2>📋 Post a listing</h2>"),
    (Pages + "LoginPage.razor", "<h1>Sign in to your account</h1>", "<h1>🔓 Sign in to your account</h1>"),
    (Pages + "SellChatPage.razor", "Chat with Agent</h1>", "💬 Chat with Agent</h1>"),
    (Pages + "ApplicantPage.razor", "Chat with Agent</h1>", "🧑 Chat with Agent</h1>"),
    (Pages + "DispatcherPage.razor", "<h1 style=\"font-size:1.4rem;margin-bottom:6px\">Agent</h1>", "<h1 style=\"font-size:1.4rem;margin-bottom:6px\">🚚 Dispatcher</h1>"),
];

var Tmp = Path.Combine(Path.GetTempPath(), $"wolfs-batch-{Guid.NewGuid():N}.jsonl");
await using (var Sw = new StreamWriter(Tmp))
{
    foreach (var (P, F, R) in Patches)
    {
        var Obj = new JsonObject { ["path"] = P, ["find"] = F, ["replace"] = R, ["idempotent"] = true };
        await Sw.WriteLineAsync(Obj.ToJsonString(new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }
}
await Console.Out.WriteLineAsync($"wrote batch file: {Tmp} ({Patches.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} patches)");

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
