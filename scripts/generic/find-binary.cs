#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

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

var Name = Get("Name")!;
var SearchRoots = (Get("SearchRoots") ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries);

foreach (var Root in SearchRoots)
{
    var R = Environment.ExpandEnvironmentVariables(Root);
    if (!Directory.Exists(R)) continue;
    foreach (var F in Directory.EnumerateFiles(R, Name, SearchOption.AllDirectories))
    {
        await Console.Out.WriteLineAsync(F);
    }
}
return 0;
