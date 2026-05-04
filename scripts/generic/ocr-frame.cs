#:property TargetFramework=net11.0-windows10.0.19041
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string? Get(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        if (After.StartsWith("@", StringComparison.Ordinal)) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}

var FramesDir = Get("FramesDir") ?? "";
var OutputPath = Get("OutputPath") ?? "";
var LangTag = Get("LangTag") ?? "en-US";
if (string.IsNullOrEmpty(FramesDir) || !Directory.Exists(FramesDir)) return 3;
if (string.IsNullOrEmpty(OutputPath)) return 4;

var Engine = OcrEngine.TryCreateFromLanguage(new Language(LangTag));
if (Engine is null) return 5;

var Results = new StringBuilder();
Results.AppendLine("{");
Results.AppendLine("  \"frames\": [");

var Files = Directory.GetFiles(FramesDir, "*.png").OrderBy(f => f).ToList();
for (var I = 0; I < Files.Count; I++)
{
    var File1 = Files[I];
    var Pad = Path.GetFileNameWithoutExtension(File1);
    try
    {
        var StorageFile = await StorageFile.GetFileFromPathAsync(File1);
        using var Stream = await StorageFile.OpenAsync(FileAccessMode.Read);
        var Decoder = await BitmapDecoder.CreateAsync(Stream);
        var Bitmap = await Decoder.GetSoftwareBitmapAsync();
        var OcrResult = await Engine.RecognizeAsync(Bitmap);
        var Text = OcrResult.Text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        Results.Append("    {\"pad\": \"");
        Results.Append(Pad);
        Results.Append("\", \"text\": \"");
        Results.Append(Text);
        Results.Append("\"}");
        if (I < Files.Count - 1) Results.Append(",");
        Results.AppendLine();
    }
    catch (Exception E)
    {
        Results.AppendLine($"    {{\"pad\": \"{Pad}\", \"error\": \"{E.Message.Replace("\"", "\\\"")}\"}}{(I < Files.Count - 1 ? "," : "")}");
    }
}

Results.AppendLine("  ]");
Results.AppendLine("}");

var OutDir = Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(OutDir)) Directory.CreateDirectory(OutDir);
await File.WriteAllTextAsync(OutputPath, Results.ToString());
return 0;
