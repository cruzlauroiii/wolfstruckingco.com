#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);
var Quote = (char)0x22;

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != Quote) continue;
        var Term = string.Concat(Quote.ToString(CultureInfo.InvariantCulture), ";");
        var End = Tail.LastIndexOf(Term, StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var FilePath = Get("FilePath")!;
if (!File.Exists(FilePath))
{
    await Console.Out.WriteLineAsync("MISSING " + FilePath);
    return 0;
}
var Fi = new FileInfo(FilePath);
await Console.Out.WriteLineAsync("size=" + Fi.Length.ToString(CultureInfo.InvariantCulture) + " mtime=" + Fi.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture) + " path=" + FilePath);
return 0;
