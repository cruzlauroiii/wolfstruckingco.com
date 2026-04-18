#:property TargetFramework=net11.0-windows
#:property UseWindowsForms=true
#:property PublishTrimmed=false
#:property IsTrimmable=false
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false

using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/add-grid.cs scripts/<add-grid-X>-config.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
var Strs = AddGridPatterns.ConstString().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
var Nums = AddGridPatterns.ConstInt().Matches(Body)
    .ToDictionary(M => M.Groups["name"].Value, M => int.Parse(M.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);

if (!Strs.TryGetValue("InputPath", out var InputPath)) { await Console.Error.WriteLineAsync("specific must declare const string InputPath"); return 3; }
if (!Strs.TryGetValue("OutputPath", out var OutputPath)) { await Console.Error.WriteLineAsync("specific must declare const string OutputPath"); return 4; }
if (!File.Exists(InputPath)) { await Console.Error.WriteLineAsync($"input not found: {InputPath}"); return 5; }
var GridStep = Nums.TryGetValue("GridStep", out var Gs) ? Gs : 100;

using var Original = (Bitmap)Image.FromFile(InputPath);
using var Out2 = new Bitmap(Original.Width, Original.Height);
using (var G = Graphics.FromImage(Out2))
{
    G.DrawImage(Original, 0, 0);
    using var GridPen = new Pen(Color.FromArgb(180, 255, 0, 255), 1);
    using var LabelFont = new Font("Consolas", 10, FontStyle.Bold);
    using var LabelBrush = new SolidBrush(Color.FromArgb(220, 255, 0, 255));
    using var LabelBg = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
    for (var X = GridStep; X < Out2.Width; X += GridStep)
    {
        G.DrawLine(GridPen, X, 0, X, Out2.Height);
        var Text = X.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var Sz = G.MeasureString(Text, LabelFont);
        G.FillRectangle(LabelBg, X + 2, 2, Sz.Width, Sz.Height);
        G.DrawString(Text, LabelFont, LabelBrush, X + 2, 2);
    }
    for (var Y = GridStep; Y < Out2.Height; Y += GridStep)
    {
        G.DrawLine(GridPen, 0, Y, Out2.Width, Y);
        var Text = Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var Sz = G.MeasureString(Text, LabelFont);
        G.FillRectangle(LabelBg, 2, Y + 2, Sz.Width, Sz.Height);
        G.DrawString(Text, LabelFont, LabelBrush, 2, Y + 2);
    }
}
Out2.Save(OutputPath, ImageFormat.Png);
await Console.Out.WriteLineAsync($"add-grid: wrote {OutputPath} ({Out2.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}x{Out2.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)}, step={GridStep.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
return 0;

namespace Scripts
{
    internal static partial class AddGridPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"const\s+int\s+(?<name>\w+)\s*=\s*(?<value>-?\d+)\s*;", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstInt();
    }
}
