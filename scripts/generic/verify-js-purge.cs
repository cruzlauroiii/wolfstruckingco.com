#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// verify-js-purge.cs - Specific. Confirms the live deploy no longer ships
// any of the deleted legacy .js files. All probe data here; delegates to
// generic fetch-url.cs --batch (item #6: thin specific, fat generic).
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scripts;

const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

// Pages to probe (path, expected-zero pattern)
(string Label, string Path, string Pattern)[] Probes =
[
    ("/Login/: no db.js script", "/Login/", "src=\"/wolfstruckingco.com/db.js"),
    ("/Login/: no wolfs-interop-shim.js", "/Login/", "wolfs-interop-shim"),
    ("/Login/: no theme.js", "/Login/", "src=\".*theme.js"),
    ("/Marketplace/: no db.js", "/Marketplace/", "src=\"/wolfstruckingco.com/db.js"),
    ("/Marketplace/: no demo.js", "/Marketplace/", "src=\".*demo.js"),
    ("/Dashboard/: no dashboard-*.js", "/Dashboard/", "src=\".*dashboard-"),
    ("/Login/: still has SSO snippet", "/Login/", "wolfs_session"),
    ("/Login/: still has SSO buttons", "/Login/", "href=\"\\?sso="),
];

var Tmp = Path.Combine(Path.GetTempPath(), $"wolfs-js-purge-{Guid.NewGuid():N}.jsonl");
var Opts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
await using (var Sw = new StreamWriter(Tmp))
{
    foreach (var (Label, Path2, Pattern) in Probes)
    {
        var Obj = new JsonObject { ["url"] = BaseUrl + Path2, ["mode"] = "grep", ["pattern"] = Pattern, ["label"] = Label };
        await Sw.WriteLineAsync(Obj.ToJsonString(Opts));
    }
}
var Psi = new ProcessStartInfo("dotnet", $"run scripts/fetch-url.cs -- --batch \"{Tmp}\"") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Paths.Repo };
using var Proc = Process.Start(Psi)!;
await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
await Proc.WaitForExitAsync();
try { File.Delete(Tmp); } catch (IOException) { }
return Proc.ExitCode;
