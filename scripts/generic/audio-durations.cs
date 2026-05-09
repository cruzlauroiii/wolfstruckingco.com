#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string? Read(string Name)
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

var Root = Read("Root");
var Pattern = Read("Pattern");
var OutputPath = Read("OutputPath");
if (Root is null || Pattern is null || OutputPath is null) return 3;
if (!Directory.Exists(Root)) return 4;

var Files = Directory.GetFiles(Root, Pattern).OrderBy(F => F, StringComparer.Ordinal).ToList();
var Lines = new List<string>();
double Total = 0;
foreach (var F in Files)
{
    var Psi = new ProcessStartInfo("ffprobe")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        ArgumentList = { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", F },
    };
    using var Proc = Process.Start(Psi);
    if (Proc is null) return 5;
    var Out = (await Proc.StandardOutput.ReadToEndAsync()).Trim();
    _ = await Proc.StandardError.ReadToEndAsync();
    await Proc.WaitForExitAsync();
    if (Proc.ExitCode != 0) return 6;
    if (!double.TryParse(Out, NumberStyles.Float, CultureInfo.InvariantCulture, out var Sec)) return 7;
    Total += Sec;
    Lines.Add(Path.GetFileName(F) + "\t" + Sec.ToString("F3", CultureInfo.InvariantCulture));
}

Lines.Add("TOTAL_FILES\t" + Files.Count.ToString(CultureInfo.InvariantCulture));
Lines.Add("TOTAL_SECONDS\t" + Total.ToString("F3", CultureInfo.InvariantCulture));
await File.WriteAllLinesAsync(OutputPath, Lines);
return 0;
