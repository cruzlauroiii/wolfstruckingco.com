#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

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
        var Term = string.Concat(Quote.ToString(), ";");
        var End = Tail.LastIndexOf(Term, StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var Folder = Get("Folder")!;
var Pattern = Get("Pattern") ?? "*.cs";
var Find = Get("Find")!;
var Replace = Get("Replace")!;

var Files = Directory.EnumerateFiles(Folder, Pattern, SearchOption.TopDirectoryOnly);
foreach (var F in Files)
{
    var Content = await File.ReadAllTextAsync(F);
    if (!Content.Contains(Find, StringComparison.Ordinal)) continue;
    var New = Content.Replace(Find, Replace, StringComparison.Ordinal);
    await File.WriteAllTextAsync(F, New);
}
return 0;
