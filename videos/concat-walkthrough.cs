#:property TargetFramework=net11.0

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
var VideosDir = Path.Combine(Repo, "docs", "videos");
var ConcatList = Path.Combine(Path.GetTempPath(), "wolfs-concat.txt");
var Output = Path.Combine(VideosDir, "walkthrough.mp4");

var Sb = new StringBuilder();
for (var N = 1; N <= 121; N++)
{
    var Pad = N.ToString("000", System.Globalization.CultureInfo.InvariantCulture);
    var Mp4 = Path.Combine(VideosDir, "scene-" + Pad + ".mp4");
    if (!File.Exists(Mp4)) { await Console.Error.WriteLineAsync("missing " + Pad); return 1; }
    Sb.Append("file '").Append(Mp4.Replace('\\', '/')).Append("'\n");
}
await File.WriteAllTextAsync(ConcatList, Sb.ToString());

var Psi = new ProcessStartInfo("ffmpeg") { UseShellExecute = false, RedirectStandardError = true };
Psi.ArgumentList.Add("-y");
Psi.ArgumentList.Add("-f");
Psi.ArgumentList.Add("concat");
Psi.ArgumentList.Add("-safe");
Psi.ArgumentList.Add("0");
Psi.ArgumentList.Add("-i");
Psi.ArgumentList.Add(ConcatList);
Psi.ArgumentList.Add("-c");
Psi.ArgumentList.Add("copy");
Psi.ArgumentList.Add("-movflags");
Psi.ArgumentList.Add("+faststart");
Psi.ArgumentList.Add(Output);
using var P = Process.Start(Psi)!;
var Err = await P.StandardError.ReadToEndAsync();
await P.WaitForExitAsync();
if (P.ExitCode != 0) { await Console.Error.WriteLineAsync(Err); return P.ExitCode; }
return 0;
