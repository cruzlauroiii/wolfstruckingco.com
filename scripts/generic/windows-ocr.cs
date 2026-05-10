#:property TargetFramework=net11.0-windows10.0.19041.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

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

var Dir = Get("Dir") ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wolfs-frames");
var Pattern = Get("Pattern") ?? "scene-*.png";
var OutDir = Get("OutDir") ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wolfs-ocr");
Directory.CreateDirectory(OutDir);

var Lang = new Language("en-US");
var Engine = OcrEngine.TryCreateFromLanguage(Lang) ?? throw new InvalidOperationException("OCR engine not available for en-US");

var Files = Directory.GetFiles(Dir, Pattern);
Array.Sort(Files, StringComparer.Ordinal);
var Done = 0;
var Failed = 0;
foreach (var Png in Files)
{
    var Name = Path.GetFileNameWithoutExtension(Png);
    var TxtPath = Path.Combine(OutDir, Name + ".txt");
    if (File.Exists(TxtPath) && File.GetLastWriteTimeUtc(TxtPath) > File.GetLastWriteTimeUtc(Png)) { Done++; continue; }
    try
    {
        var File1 = await StorageFile.GetFileFromPathAsync(Png);
        using var Stream = await File1.OpenAsync(FileAccessMode.Read);
        var Decoder = await BitmapDecoder.CreateAsync(Stream);
        using var SoftBitmap = await Decoder.GetSoftwareBitmapAsync();
        var Result = await Engine.RecognizeAsync(SoftBitmap);
        await File.WriteAllTextAsync(TxtPath, Result.Text ?? string.Empty);
        Done++;
    }
    catch (Exception)
    {
        Failed++;
    }
}
await Console.Error.WriteLineAsync("windows-ocr: done=" + Done.ToString(CultureInfo.InvariantCulture) + " failed=" + Failed.ToString(CultureInfo.InvariantCulture));
return Failed > 0 ? 4 : 0;
