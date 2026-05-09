#:property TargetFramework=net11.0

using System.Diagnostics;
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

var TargetFile = Get("TargetFile")!;
if (!File.Exists(TargetFile))
{
    return 2;
}

var Psi = new ProcessStartInfo("ffprobe")
{
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
foreach (var A in new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", TargetFile })
{
    Psi.ArgumentList.Add(A);
}

using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync();
var Err = await P.StandardError.ReadToEndAsync();
await P.WaitForExitAsync();

if (P.ExitCode != 0)
{
    await Console.Error.WriteLineAsync("ffprobe error: " + Err);
    return 3;
}

var Trimmed = Out.Trim();
if (!double.TryParse(Trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var Seconds))
{
    await Console.Error.WriteLineAsync("could not parse duration: " + Trimmed);
    return 4;
}

var Min = (int)(Seconds / 60);
var Sec = Seconds - (Min * 60);
Console.WriteLine("duration_seconds=" + Seconds.ToString("F2", CultureInfo.InvariantCulture));
Console.WriteLine("duration_mmss=" + Min.ToString(CultureInfo.InvariantCulture) + ":" + Sec.ToString("F1", CultureInfo.InvariantCulture).PadLeft(4, '0'));
return 0;
