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

var ScenesDir = Get("ScenesDir")!;
var MaxPadStr = Get("MaxPad")!;
var MaxPad = int.Parse(MaxPadStr, CultureInfo.InvariantCulture);

var Files = Directory.GetFiles(ScenesDir, "scene-*.mp4");
var Deleted = 0;
foreach (var F in Files)
{
    var Name = Path.GetFileNameWithoutExtension(F);
    var PadPart = Name.StartsWith("scene-", StringComparison.Ordinal) ? Name[6..] : Name;
    var DigitsOnly = PadPart.TrimEnd('a');
    if (!int.TryParse(DigitsOnly, NumberStyles.Integer, CultureInfo.InvariantCulture, out var N))
    {
        continue;
    }

    if (N > MaxPad)
    {
        File.Delete(F);
        Deleted++;
        await Console.Error.WriteLineAsync("deleted " + Path.GetFileName(F));
    }
}

await Console.Error.WriteLineAsync("deleted " + Deleted.ToString(CultureInfo.InvariantCulture) + " orphan scene mp4s");
return 0;
