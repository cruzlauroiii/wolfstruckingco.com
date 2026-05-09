#:property TargetFramework=net11.0

if (args.Length < 1)
{
    return 1;
}

var Spec = await File.ReadAllLinesAsync(args[0]);

string Get(string Name)
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

    return string.Empty;
}

var Target = Get("Target");
if (string.IsNullOrEmpty(Target))
{
    return 2;
}

if (Directory.Exists(Target))
{
    foreach (var Fpath in Directory.GetFiles(Target, "*", SearchOption.AllDirectories))
    {
        File.SetAttributes(Fpath, FileAttributes.Normal);
    }

    Directory.Delete(Target, recursive: true);
}

return 0;
