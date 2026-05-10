#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
using System.Net.Http;

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

var Url = Get("Url")!;
var Keywords = Get("Keywords") ?? "";

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
Http.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
Http.DefaultRequestHeaders.Add("Pragma", "no-cache");
var Body = await Http.GetStringAsync(Url);
await Console.Error.WriteLineAsync("fetch-and-grep: head[0..400]=" + (Body.Length > 400 ? Body[..400] : Body));
await Console.Error.WriteLineAsync("fetch-and-grep: url=" + Url);
await Console.Error.WriteLineAsync("fetch-and-grep: bytes=" + Body.Length.ToString(CultureInfo.InvariantCulture));
foreach (var K in Keywords.Split(','))
{
    var Kt = K.Trim();
    if (Kt.Length == 0) continue;
    var Hit = Body.Contains(Kt, StringComparison.OrdinalIgnoreCase);
    await Console.Error.WriteLineAsync("fetch-and-grep: kw='" + Kt + "' " + (Hit ? "HIT" : "miss"));
}
return 0;
