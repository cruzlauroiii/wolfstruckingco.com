#:property TargetFramework=net11.0-windows
#:property PublishTrimmed=false
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:package System.Drawing.Common@9.0.0

using System.Drawing;
using System.Drawing.Imaging;

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

var InputPath = Get("InputPath") ?? "";
var OutputPath = Get("OutputPath") ?? "";
var Step = int.Parse(Get("Step") ?? "100");
if (string.IsNullOrEmpty(InputPath) || !File.Exists(InputPath)) return 3;
if (string.IsNullOrEmpty(OutputPath)) return 4;

using var Src = Image.FromFile(InputPath);
using var Bmp = new Bitmap(Src.Width, Src.Height);
using var G = Graphics.FromImage(Bmp);
G.DrawImage(Src, 0, 0);
using var GridPen = new Pen(Color.FromArgb(180, 255, 0, 0), 1);
using var BoldPen = new Pen(Color.FromArgb(220, 255, 0, 0), 2);
using var Font = new Font("Consolas", 11, FontStyle.Bold);
using var Bg = new SolidBrush(Color.FromArgb(220, 0, 0, 0));
using var Fg = new SolidBrush(Color.Yellow);

for (int X = 0; X < Bmp.Width; X += Step)
{
    G.DrawLine(X % (Step * 5) == 0 ? BoldPen : GridPen, X, 0, X, Bmp.Height);
    var Lbl = X.ToString();
    var Sz = G.MeasureString(Lbl, Font);
    G.FillRectangle(Bg, X + 2, 2, Sz.Width, Sz.Height);
    G.DrawString(Lbl, Font, Fg, X + 2, 2);
}
for (int Y = 0; Y < Bmp.Height; Y += Step)
{
    G.DrawLine(Y % (Step * 5) == 0 ? BoldPen : GridPen, 0, Y, Bmp.Width, Y);
    var Lbl = Y.ToString();
    var Sz = G.MeasureString(Lbl, Font);
    G.FillRectangle(Bg, 2, Y + 2, Sz.Width, Sz.Height);
    G.DrawString(Lbl, Font, Fg, 2, Y + 2);
}

Bmp.Save(OutputPath, ImageFormat.Png);
return 0;
