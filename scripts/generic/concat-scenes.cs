#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.Json;

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
        bool Verbatim = After.StartsWith("@", StringComparison.Ordinal);
        if (Verbatim) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}

var Docs = Get("Docs")!;
var ScenesPath = Get("ScenesPath")!;
var ExtrasPath = Get("ExtrasPath") ?? "";
var Output = Get("Output")!;

string PadFor(JsonElement Sc)
{
    if (Sc.TryGetProperty("pad", out var P) && P.ValueKind == JsonValueKind.String)
    {
        var Pp = P.GetString();
        if (!string.IsNullOrEmpty(Pp)) return Pp;
    }
    var T = Sc.GetProperty("target").GetString() ?? "";
    if (T.Contains("cb=", StringComparison.Ordinal))
    {
        var Cb = T.Split("cb=")[^1].Replace("?", "", StringComparison.Ordinal).Replace("/", "", StringComparison.Ordinal).Replace("&theme=light", "", StringComparison.Ordinal).Trim();
        var Amp = Cb.IndexOf('&');
        if (Amp > 0) Cb = Cb.Substring(0, Amp);
        return Cb;
    }
    return "";
}

var MainScenes = JsonDocument.Parse(await File.ReadAllTextAsync(ScenesPath)).RootElement;
var Extras = new Dictionary<string, List<string>>(StringComparer.Ordinal);
if (!string.IsNullOrEmpty(ExtrasPath) && File.Exists(ExtrasPath))
{
    foreach (var X in JsonDocument.Parse(await File.ReadAllTextAsync(ExtrasPath)).RootElement.EnumerateArray())
    {
        var After = X.GetProperty("insertAfter").GetString()!;
        var Pad = X.TryGetProperty("pad", out var Pp) ? Pp.GetString()! : PadFor(X);
        if (!Extras.ContainsKey(After)) Extras[After] = new List<string>();
        Extras[After].Add(Pad);
    }
}

var Order = new List<string>();
foreach (var Sc in MainScenes.EnumerateArray())
{
    var P = PadFor(Sc);
    if (string.IsNullOrEmpty(P)) continue;
    Order.Add(P);
    if (Extras.TryGetValue(P, out var Ex))
    {
        foreach (var Ep in Ex) Order.Add(Ep);
    }
}

var Mp4Paths = new List<string>();
var Missing = new List<string>();
foreach (var P in Order)
{
    var Mp4 = Path.Combine(Docs, $"scene-{P}.mp4");
    if (File.Exists(Mp4) && new FileInfo(Mp4).Length > 0) Mp4Paths.Add(Mp4);
    else Missing.Add(P);
}

if (Missing.Count > 0) await Console.Error.WriteLineAsync($"missing scenes: {string.Join(",", Missing)}");
if (Mp4Paths.Count == 0) return 3;

var ConcatTxt = Path.Combine(Path.GetTempPath(), "concat-scenes.txt");
await File.WriteAllLinesAsync(ConcatTxt, Mp4Paths.Select(m => $"file '{m.Replace("\\", "/", StringComparison.Ordinal)}'"));

try { File.Delete(Output); } catch { }
var Psi = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-y", "-f", "concat", "-safe", "0", "-i", ConcatTxt, "-c", "copy", Output }) Psi.ArgumentList.Add(A);
using var P2 = Process.Start(Psi)!;
var Eo = P2.StandardOutput.ReadToEndAsync();
var Ee = P2.StandardError.ReadToEndAsync();
var Ex2 = P2.WaitForExitAsync();
if (await Task.WhenAny(Ex2, Task.Delay(300000)) != Ex2) { try { P2.Kill(true); } catch { } return 4; }
await Task.WhenAll(Eo, Ee);
if (P2.ExitCode != 0)
{
    await Console.Error.WriteLineAsync(await Ee);
    return P2.ExitCode;
}
Console.WriteLine($"concat ok: {Mp4Paths.Count} scenes -> {Output} ({new FileInfo(Output).Length} bytes)");
return 0;
