#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != '\u0022') continue;
        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var BaseUrl = Get("BaseUrl")!;
var OutputFile = Get("OutputFile")!;
var ServeUrl = Get("ServeUrl") ?? "http://127.0.0.1:9334/";
var Repo = Get("Repo") ?? @"C:\repo\public\wolfstruckingco.com";
var Routes = (Get("Routes") ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries);

var ServePsi = new ProcessStartInfo("dotnet")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    WorkingDirectory = Repo,
    CreateNoWindow = false,
};
ServePsi.ArgumentList.Add("run");
ServePsi.ArgumentList.Add(@"main\scripts\generic\chrome-devtools.cs");
ServePsi.ArgumentList.Add(@"main\scripts\specific\chrome-devtools-serve-config.cs");
var Serve = Process.Start(ServePsi)!;

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
async Task<string> Post(string Cmd)
{
    using var Req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, new Uri(ServeUrl)) { Content = new StringContent(Cmd) };
    using var R = await Http.SendAsync(Req).ConfigureAwait(false);
    return await R.Content.ReadAsStringAsync().ConfigureAwait(false);
}

var Ready = false;
for (var I = 0; I < 120; I++)
{
    try
    {
        var R = await Post("list_pages").ConfigureAwait(false);
        if (!string.IsNullOrEmpty(R)) { Ready = true; break; }
    }
    catch { }
    await Task.Delay(500).ConfigureAwait(false);
}
if (!Ready)
{
    try { Serve.Kill(true); } catch { }
    await File.WriteAllTextAsync(OutputFile, "serve daemon never became ready\n").ConfigureAwait(false);
    return 2;
}

await Post("clear_cache").ConfigureAwait(false);
await Task.Delay(500).ConfigureAwait(false);

var Report = new StringBuilder();
var Total = 0;
try
{
    for (var I = 0; I < Routes.Length; I++)
    {
        var Route = Routes[I].Trim();
        var Url = BaseUrl + Route;
        Report.Append("=== [").Append((I + 1).ToString(CultureInfo.InvariantCulture)).Append('/').Append(Routes.Length.ToString(CultureInfo.InvariantCulture)).Append("] ").Append(Route).AppendLine(" ===");
        await Post("new_page --url " + Url).ConfigureAwait(false);
        await Task.Delay(1500).ConfigureAwait(false);
        var Hyd = false;
        for (var W = 0; W < 30; W++)
        {
            var R = await Post("evaluate_script --function \u0022() => typeof window.WolfsInterop\u0022").ConfigureAwait(false);
            if (R.Contains("object", StringComparison.Ordinal)) { Hyd = true; break; }
            await Task.Delay(400).ConfigureAwait(false);
        }
        Report.Append("hydrated: ").AppendLine(Hyd ? "true" : "false");
        await Task.Delay(800).ConfigureAwait(false);
        var Msgs = await Post("list_console_messages").ConfigureAwait(false);
        var Errs = new List<string>();
        foreach (var L in Msgs.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(L)) continue;
            var Lower = L.ToLowerInvariant();
            if (Lower.Contains("error", StringComparison.Ordinal) || Lower.Contains("uncaught", StringComparison.Ordinal) || Lower.Contains("failed to", StringComparison.Ordinal))
            {
                Errs.Add(L.Trim());
            }
        }
        if (Errs.Count == 0) { Report.AppendLine("OK"); }
        else { Total += Errs.Count; foreach (var E in Errs) Report.Append("  ! ").AppendLine(E); }
    }
    Report.Append("\nTOTAL ERRORS: ").AppendLine(Total.ToString(CultureInfo.InvariantCulture));
}
finally
{
    try { Serve.Kill(true); } catch { }
    Serve.Dispose();
    await File.WriteAllTextAsync(OutputFile, Report.ToString()).ConfigureAwait(false);
}
return Total > 0 ? 5 : 0;
