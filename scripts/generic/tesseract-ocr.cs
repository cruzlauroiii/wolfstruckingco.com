#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
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

var Dir = Get("Dir")!;
var Pattern = Get("Pattern") ?? "scene-*.png";
var OutDir = Get("OutDir")!;
var TesseractExe = Get("TesseractExe") ?? "tesseract";
Directory.CreateDirectory(OutDir);

var Files = Directory.GetFiles(Dir, Pattern);
Array.Sort(Files, StringComparer.Ordinal);
var Done = 0;
var Failed = 0;
foreach (var Png in Files)
{
    var Name = Path.GetFileNameWithoutExtension(Png);
    var TxtPath = Path.Combine(OutDir, Name + ".txt");
    if (File.Exists(TxtPath)) { Done++; continue; }
    var Psi = new ProcessStartInfo(TesseractExe)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        ArgumentList = { Png, "stdout", "-l", "eng", "--psm", "6" },
    };
    using var P = Process.Start(Psi)!;
    var Out = await P.StandardOutput.ReadToEndAsync();
    _ = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    if (P.ExitCode == 0)
    {
        await File.WriteAllTextAsync(TxtPath, Out);
        Done++;
    }
    else
    {
        Failed++;
    }
}
await Console.Error.WriteLineAsync("tesseract-ocr: done=" + Done.ToString(CultureInfo.InvariantCulture) + " failed=" + Failed.ToString(CultureInfo.InvariantCulture));
return Failed > 0 ? 4 : 0;
