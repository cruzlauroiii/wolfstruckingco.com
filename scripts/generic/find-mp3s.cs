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

var Roots = (Get("Roots") ?? "").Split(';');
foreach (var Root in Roots)
{
    var R = Root.Trim();
    if (R.Length == 0) continue;
    if (!Directory.Exists(R)) { await Console.Error.WriteLineAsync("miss: " + R); continue; }
    int Count = 0;
    long TotalBytes = 0;
    var Sample = new List<string>();
    foreach (var F in Directory.EnumerateFiles(R, "*.mp3", SearchOption.AllDirectories))
    {
        Count++;
        var Fi = new FileInfo(F);
        TotalBytes += Fi.Length;
        if (Sample.Count < 5) Sample.Add(F);
    }
    await Console.Error.WriteLineAsync("found " + Count.ToString(CultureInfo.InvariantCulture) + " mp3s in " + R + " (" + (TotalBytes / 1024).ToString(CultureInfo.InvariantCulture) + "KB)");
    foreach (var S in Sample) await Console.Error.WriteLineAsync("  " + S);
}
return 0;
