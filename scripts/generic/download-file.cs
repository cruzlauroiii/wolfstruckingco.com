#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Net.Http;

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

var Url = Get("Url") ?? "";
var OutputPath = Get("OutputPath") ?? "";
if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(OutputPath)) return 3;

var Dir = Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(Dir)) Directory.CreateDirectory(Dir);

if (File.Exists(OutputPath) && new FileInfo(OutputPath).Length > 0)
{
    Console.WriteLine($"already exists: {OutputPath} ({new FileInfo(OutputPath).Length} bytes)");
    return 0;
}

using var Http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
Http.DefaultRequestHeaders.UserAgent.ParseAdd("download-file/1.0");
var Resp = await Http.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);
Resp.EnsureSuccessStatusCode();
var Total = Resp.Content.Headers.ContentLength ?? -1;
using var In = await Resp.Content.ReadAsStreamAsync();
using var Out = File.Create(OutputPath);
var Buf = new byte[64 * 1024];
long Read = 0;
int N;
while ((N = await In.ReadAsync(Buf)) > 0)
{
    await Out.WriteAsync(Buf.AsMemory(0, N));
    Read += N;
}
Console.WriteLine($"downloaded {Read} bytes to {OutputPath}");
return 0;
