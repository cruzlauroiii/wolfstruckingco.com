#:property TargetFramework=net11.0

if (args.Length < 1)
{
    return 1;
}

var Specs = await File.ReadAllLinesAsync(args[0]);

string Get(string Name)
{
    foreach (var Line in Specs)
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

    return string.Empty;
}

var Source = Get("Source");
var Dest = Get("Dest");

if (string.IsNullOrEmpty(Source) || !Directory.Exists(Source))
{
    return 2;
}

if (Directory.Exists(Dest))
{
    Directory.Delete(Dest, recursive: true);
}

Directory.CreateDirectory(Dest);
foreach (var Dir in Directory.GetDirectories(Source, "*", SearchOption.AllDirectories))
{
    Directory.CreateDirectory(Dir.Replace(Source, Dest, StringComparison.Ordinal));
}

foreach (var Fpath in Directory.GetFiles(Source, "*", SearchOption.AllDirectories))
{
    File.Copy(Fpath, Fpath.Replace(Source, Dest, StringComparison.Ordinal), overwrite: true);
}

return 0;
