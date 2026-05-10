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

var Docs = Get("Docs")!;
var Pads = Get("Pads")!;

var Deleted = 0;
var Missing = 0;
foreach (var Raw in Pads.Split(','))
{
    var P = Raw.Trim();
    if (P.Length == 0) continue;
    var Mp4 = Path.Combine(Docs, "scene-" + P + ".mp4");
    if (File.Exists(Mp4)) { File.Delete(Mp4); Deleted++; } else { Missing++; }
}
await Console.Error.WriteLineAsync("delete-pads: deleted=" + Deleted.ToString(CultureInfo.InvariantCulture) + " missing=" + Missing.ToString(CultureInfo.InvariantCulture));
return 0;
