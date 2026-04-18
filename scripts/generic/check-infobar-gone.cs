#:property TargetFramework=net11.0-windows10.0.19041.0
#:property UseWindowsForms=true
#:property PublishTrimmed=false
#:property IsTrimmable=false
#:property PublishAot=false
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

var Bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
using var Bm = new Bitmap(Bounds.Width, Bounds.Height);
using (var Gr = Graphics.FromImage(Bm)) { Gr.CopyFromScreen(Bounds.Location, Point.Empty, Bounds.Size); }
var Tmp = Path.Combine(Path.GetTempPath(), "wolfs-infobar-check.png");
Bm.Save(Tmp, ImageFormat.Png);

var Engine = OcrEngine.TryCreateFromLanguage(new Language("en-US")) ?? OcrEngine.TryCreateFromUserProfileLanguages();
if (Engine == null) { return 0; }
var Sf = await StorageFile.GetFileFromPathAsync(Tmp);
using var Stream = await Sf.OpenAsync(FileAccessMode.Read);
var Decoder = await BitmapDecoder.CreateAsync(Stream);
var Sb = await Decoder.GetSoftwareBitmapAsync();
var Result = await Engine.RecognizeAsync(Sb);
var Text = (Result.Text ?? "").ToLowerInvariant();
if (Text.Contains("controlled by automated", StringComparison.Ordinal) || Text.Contains("turn off in settings", StringComparison.Ordinal))
{
    return 1;
}
return 0;
