#:property TargetFramework=net11.0

using System.Globalization;

if (args.Length < 1)
{
    return 1;
}

var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0)
        {
            continue;
        }

        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@')
        {
            Tail = Tail[1..];
        }

        if (Tail.Length == 0 || Tail[0] != '\u0022')
        {
            continue;
        }

        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1)
        {
            continue;
        }

        return Tail[1..End];
    }

    return null;
}

var Dir = Get("Dir")!;
var Pattern = Get("Pattern")!;
var Files = Directory.GetFiles(Dir, Pattern);
Array.Sort(Files, StringComparer.Ordinal);
Console.WriteLine("count=" + Files.Length.ToString(CultureInfo.InvariantCulture));
foreach (var F in Files.Take(3))
{
    Console.WriteLine("first: " + Path.GetFileName(F));
}

for (var I = Files.Length - 3; I < Files.Length && I >= 0; I++)
{
    Console.WriteLine("last: " + Path.GetFileName(Files[I]));
}

return 0;
