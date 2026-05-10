#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

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

var FilePath = Get("FilePath")!;
var MaxBytes = int.Parse(Get("MaxBytes") ?? "4000", CultureInfo.InvariantCulture);
if (!File.Exists(FilePath)) { await Console.Error.WriteLineAsync("missing: " + FilePath); return 2; }
var Bytes = await File.ReadAllBytesAsync(FilePath);
await Console.Error.WriteLineAsync("read " + FilePath + " bytes=" + Bytes.Length.ToString(CultureInfo.InvariantCulture));
var Slice = Bytes.Length > MaxBytes ? Bytes[..MaxBytes] : Bytes;
await Console.Error.WriteLineAsync(System.Text.Encoding.UTF8.GetString(Slice));
return 0;
