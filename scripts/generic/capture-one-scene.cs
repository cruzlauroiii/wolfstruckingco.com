#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string Unesc(string Raw)
{
    var Sb = new System.Text.StringBuilder(Raw.Length);
    for (int I = 0; I < Raw.Length; I++)
    {
        if (Raw[I] == '\\' && I + 1 < Raw.Length)
        {
            char N = Raw[I + 1];
            if (N == '"') { Sb.Append('"'); I++; }
            else if (N == '\\') { Sb.Append('\\'); I++; }
            else if (N == 'n') { Sb.Append('\n'); I++; }
            else if (N == 't') { Sb.Append('\t'); I++; }
            else if (N == 'r') { Sb.Append('\r'); I++; }
            else if (N == '\'') { Sb.Append('\''); I++; }
            else Sb.Append(Raw[I]);
        }
        else Sb.Append(Raw[I]);
    }
    return Sb.ToString();
}

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
        var Raw = After.Substring(1, End - 1);
        return Verbatim ? Raw : Unesc(Raw);
    }
    return null;
}

var Repo = Get("Repo")!;
var Url = Get("Url")!;
var PngPath = Get("PngPath")!;
var Mp3Path = Get("Mp3Path")!;
var Mp4Path = Get("Mp4Path")!;
var Pad = Get("Pad")!;
var WaitMsRaw = Get("WaitMs");
var WaitMs = int.Parse(string.IsNullOrEmpty(WaitMsRaw) ? "10000" : WaitMsRaw);

Directory.CreateDirectory(Path.GetDirectoryName(PngPath)!);
Directory.CreateDirectory(Path.GetDirectoryName(Mp4Path)!);

var ServeHttp = new System.Net.Http.HttpClient();
ServeHttp.Timeout = TimeSpan.FromSeconds(60);
Process? ServeProc = null;
async Task EnsureServeAsync()
{
    if (ServeProc != null && !ServeProc.HasExited) return;
    try { using var Probe = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) }; var R = await Probe.PostAsync(new Uri("http://127.0.0.1:9334/"), new System.Net.Http.StringContent("list_pages")); if (R.IsSuccessStatusCode) { return; } } catch { }
    var Cfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        public const string Command = \"serve\";\n    }\n}\n";
    var Tmp = Path.Combine(Path.GetTempPath(), $"serve-{Guid.NewGuid():N}.cs");
    await File.WriteAllTextAsync(Tmp, Cfg);
    var P = new ProcessStartInfo("dotnet") { WorkingDirectory = Repo, UseShellExecute = false };
    P.ArgumentList.Add("run");
    P.ArgumentList.Add(Path.Combine(Repo, "scripts", "generic", "chrome-devtools.cs"));
    P.ArgumentList.Add(Tmp);
    ServeProc = Process.Start(P)!;
    await Task.Delay(15000);
}

async Task<string> PostServeAsync(string Ln)
{
    try { using var Resp = await ServeHttp.PostAsync("http://127.0.0.1:9334/", new System.Net.Http.StringContent(Ln)); return await Resp.Content.ReadAsStringAsync(); } catch { return ""; }
}

await EnsureServeAsync();

await PostServeAsync("new_page --url \"" + Url + "\"");
await Task.Delay(WaitMs);

var Listing = await PostServeAsync("list_pages");
var PageIdx = "1";
foreach (var Line in Listing.Split('\n'))
{
    var T = Line.Trim();
    if (!T.Contains("Apply", StringComparison.OrdinalIgnoreCase) && !T.Contains("localhost", StringComparison.OrdinalIgnoreCase) && !T.Contains("wolfstruckingco", StringComparison.OrdinalIgnoreCase)) continue;
    var Colon = T.IndexOf(':');
    if (Colon < 1) continue;
    var Idx = T.Substring(0, Colon).Trim();
    if (Idx.All(char.IsDigit)) { PageIdx = Idx; break; }
}

try { File.Delete(PngPath); } catch { }
foreach (var TryIdx in new[] { PageIdx, "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" })
{
    await PostServeAsync("take_screenshot --pageId \"" + TryIdx + "\" --filePath \"" + PngPath + "\"");
    await Task.Delay(800);
    if (File.Exists(PngPath) && new FileInfo(PngPath).Length > 0) break;
    try { File.Delete(PngPath); } catch { }
}

try { ServeProc?.Kill(true); } catch { }

if (!File.Exists(PngPath) || new FileInfo(PngPath).Length == 0) return 3;
if (!File.Exists(Mp3Path)) return 4;

try { File.Delete(Mp4Path); } catch { }
var Ff = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-y", "-loop", "1", "-i", PngPath, "-i", Mp3Path, "-c:v", "libx264", "-tune", "stillimage", "-pix_fmt", "yuv420p", "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30", "-c:a", "aac", "-b:a", "128k", "-ar", "44100", "-shortest", Mp4Path }) Ff.ArgumentList.Add(A);
using var Fp = Process.Start(Ff)!;
var Oe = Fp.StandardOutput.ReadToEndAsync();
var Ee = Fp.StandardError.ReadToEndAsync();
var Ex = Fp.WaitForExitAsync();
var Winner = await Task.WhenAny(Ex, Task.Delay(120000));
if (Winner != Ex) { try { Fp.Kill(true); } catch { } return 5; }
await Task.WhenAll(Oe, Ee);
return Fp.ExitCode;
