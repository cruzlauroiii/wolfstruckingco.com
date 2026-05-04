#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Net.Http;
using System.Text;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string? Get(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        if (After.StartsWith("@", StringComparison.Ordinal)) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}

var WorkerUrl = Get("WorkerUrl") ?? "";
var OutputPath = Get("OutputPath") ?? "";
if (string.IsNullOrEmpty(WorkerUrl) || string.IsNullOrEmpty(OutputPath)) return 3;

var Payload = "{\"messages\":[{\"role\":\"user\",\"content\":\"Dispatch QA: a driver narration says 'Driver from China taps Apply' but the screen actually renders 'Wolfs Home, Want to drive for Wolfs?, Marketplace, Apply to drive'. Score how well the narration matches the rendered UI on 0.0-1.0, and write a better narration the dispatcher would record. Reply with exactly: {\\\"score\\\": 0.0-1.0, \\\"rewrite\\\": \\\"...\\\"}\"}],\"system\":\"You are the Wolfs Trucking dispatch QA assistant. You audit how well our recorded narrations match the on-screen UI. Reply only with the requested JSON object.\",\"max_tokens\":128}";
using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
var Req = new HttpRequestMessage(HttpMethod.Post, WorkerUrl.TrimEnd('/') + "/ai")
{
    Content = new StringContent(Payload, Encoding.UTF8, "application/json"),
};
Req.Headers.Add("X-Wolfs-Session", "probe");
Req.Headers.Add("X-Wolfs-Role", "admin");
var Resp = await Http.SendAsync(Req);
var Body = await Resp.Content.ReadAsStringAsync();
var Out = $"STATUS: {(int)Resp.StatusCode} {Resp.StatusCode}\nHEADERS:\n{Resp.Headers}\nBODY:\n{Body}\n";
var Dir = Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(Dir)) Directory.CreateDirectory(Dir);
await File.WriteAllTextAsync(OutputPath, Out);
return 0;
