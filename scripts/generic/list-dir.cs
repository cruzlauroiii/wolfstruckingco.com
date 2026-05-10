#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;

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

var Dir = Get("Dir")!;
var Pattern = Get("Pattern") ?? "*";
var Limit = int.Parse(Get("Limit") ?? "50", CultureInfo.InvariantCulture);
if (!Directory.Exists(Dir)) { await Console.Error.WriteLineAsync("missing dir: " + Dir); return 2; }
var Files = Directory.EnumerateFiles(Dir, Pattern).OrderBy(f => f).ToList();
await Console.Error.WriteLineAsync("count=" + Files.Count.ToString(CultureInfo.InvariantCulture));
foreach (var F in Files.Take(Limit))
{
    await Console.Error.WriteLineAsync("  " + Path.GetFileName(F) + " (" + new FileInfo(F).Length + "B)");
}
return 0;
