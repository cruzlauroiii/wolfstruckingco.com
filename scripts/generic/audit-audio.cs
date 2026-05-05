#:property TargetFramework=net11.0

using System.Diagnostics;

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

var SourceDir = Get("SourceDir");
var Pattern = Get("Pattern");
var OutPath = Get("OutPath");

if (!Directory.Exists(SourceDir))
{
    return 2;
}

var Files = Directory.GetFiles(SourceDir, Pattern).Order(StringComparer.Ordinal).ToArray();
var Lines = new List<string>();
foreach (var F in Files)
{
    var Psi = new ProcessStartInfo("ffprobe")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    foreach (var Arg in new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", F })
    {
        Psi.ArgumentList.Add(Arg);
    }

    using var Proc = Process.Start(Psi);
    if (Proc is null)
    {
        continue;
    }

    var Out = await Proc.StandardOutput.ReadToEndAsync();
    _ = await Proc.StandardError.ReadToEndAsync();
    await Proc.WaitForExitAsync();
    Lines.Add(Out.Trim() + "\t" + Path.GetFileName(F));
}

await File.WriteAllLinesAsync(OutPath, Lines);
return 0;
