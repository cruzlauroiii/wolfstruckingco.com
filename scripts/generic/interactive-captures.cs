#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;

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

var Repo = Get("Repo")!;
var ScenesJsonPath = Get("ScenesJsonPath")!;
var AudioDir = Get("AudioDir")!;
var FramesDir = Get("FramesDir")!;
var OutDir = Get("OutDir")!;
var ServeUrl = Get("ServeUrl") ?? "http://127.0.0.1:9334/";
var Concurrency = int.Parse(Get("Concurrency") ?? "4", CultureInfo.InvariantCulture);

Directory.CreateDirectory(FramesDir);
Directory.CreateDirectory(OutDir);

var Json = await File.ReadAllTextAsync(ScenesJsonPath);
using var Doc = JsonDocument.Parse(Json);
var Scenes = Doc.RootElement.EnumerateArray().ToList();

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
async Task<string> Post(string Cmd) { try { var R = await Http.PostAsync(new Uri(ServeUrl), new StringContent(Cmd)); return await R.Content.ReadAsStringAsync(); } catch (Exception E) { return "ERR:" + E.Message; } }

var ProbeOk = false;
for (var I = 0; I < 30; I++)
{
    var R = await Post("list_pages");
    if (!R.StartsWith("ERR:", StringComparison.Ordinal)) { ProbeOk = true; break; }
    await Task.Delay(2000);
}
if (!ProbeOk)
{
    await Console.Error.WriteLineAsync("serve daemon unreachable on " + ServeUrl);
    return 2;
}

var Sem = new SemaphoreSlim(Concurrency);
var Done = 0;
var Failed = 0;
var Tasks = new List<Task>();
foreach (var Scene in Scenes)
{
    var Url = Scene.GetProperty("target").GetString() ?? string.Empty;
    var Sso = Scene.TryGetProperty("sso", out var SsoEl) ? SsoEl.GetString() ?? string.Empty : string.Empty;
    var Pad = string.Empty;
    var CbIdx = Url.IndexOf("cb=", StringComparison.Ordinal);
    if (CbIdx >= 0)
    {
        var Start = CbIdx + 3;
        var Stop = Start;
        while (Stop < Url.Length && (char.IsDigit(Url[Stop]) || (Url[Stop] >= 'a' && Url[Stop] <= 'z'))) Stop++;
        Pad = Url[Start..Stop];
    }
    if (string.IsNullOrEmpty(Pad) || string.IsNullOrEmpty(Url)) continue;
    var ChatMsg = Scene.TryGetProperty("chat", out var ChEl) ? ChEl.GetString() ?? string.Empty : string.Empty;
    var Mp4 = Path.Combine(OutDir, $"scene-{Pad}.mp4");
    var Png = Path.Combine(FramesDir, $"scene-{Pad}.png");
    var Mp3 = Path.Combine(AudioDir, $"scene-{Pad}.mp3");
    Tasks.Add(Task.Run(async () =>
    {
        await Sem.WaitAsync();
        try
        {
            await Post("new_page --url " + Url);
            await Task.Delay(2500);
            for (var Wi = 0; Wi < 25; Wi++)
            {
                var Wr = await Post("evaluate_script --function \u0022() => typeof window.WolfsInterop\u0022");
                if (Wr.Contains("object", StringComparison.Ordinal))
                {
                    break;
                }
                await Task.Delay(400);
            }
            await Post("evaluate_script --function \u0022() => { try { localStorage.setItem('wolfs_theme','light'); document.documentElement.setAttribute('data-theme','light'); if (window.WolfsInterop && window.WolfsInterop.themeWrite) window.WolfsInterop.themeWrite('light'); } catch(e){} return 'themed'; }\u0022");
            await Task.Delay(800);
            if (!string.IsNullOrEmpty(Sso))
            {
                var ClickJs = "() => { const a = Array.from(document.querySelectorAll('a')).find(x => new RegExp('" + Sso + "','i').test(x.textContent) && /oauth|google|github|microsoft|okta/i.test(x.href||'')); if (a) { window.location.href = a.href; return 'sso_click'; } return 'no_sso_link'; }";
                await Post("evaluate_script --function \u0022" + ClickJs.Replace("\u0022", "\\\u0022") + "\u0022");
                await Task.Delay(3000);
            }
            if (!string.IsNullOrEmpty(ChatMsg))
            {
                var TypeJs = "() => { const i = document.querySelector('.WChat input, .WChat textarea, input[type=text], textarea'); if (!i) return 'no_input'; i.focus(); i.value = '" + ChatMsg.Replace("'", "\\'") + "'; i.dispatchEvent(new Event('input', {bubbles:true})); const b = document.querySelector('.WChat button, button[type=submit]'); if (b) b.click(); return 'typed'; }";
                await Post("evaluate_script --function \u0022" + TypeJs.Replace("\u0022", "\\\u0022") + "\u0022");
                await Task.Delay(2500);
            }
            await Post("take_screenshot --filePath \u0022" + Png.Replace("\\", "/") + "\u0022 --fullPage true");
            await Task.Delay(500);
            if (!File.Exists(Png)) { Interlocked.Increment(ref Failed); return; }
            var Psi = new ProcessStartInfo("ffmpeg") { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
            var HasMp3 = File.Exists(Mp3);
            var FfArgs = HasMp3 ? new[] { "-y", "-loop", "1", "-i", Png, "-i", Mp3, "-c:v", "libx264", "-tune", "stillimage", "-c:a", "aac", "-shortest", "-pix_fmt", "yuv420p", "-vf", "scale=1920:1080,format=yuv420p", "-r", "24", Mp4 } : new[] { "-y", "-loop", "1", "-i", Png, "-c:v", "libx264", "-t", "3", "-pix_fmt", "yuv420p", "-vf", "scale=1920:1080,format=yuv420p", "-r", "24", Mp4 };
            foreach (var A in FfArgs) Psi.ArgumentList.Add(A);
            using var P = Process.Start(Psi)!;
            _ = await P.StandardError.ReadToEndAsync();
            await P.WaitForExitAsync();
            if (P.ExitCode == 0 && File.Exists(Mp4)) Interlocked.Increment(ref Done); else Interlocked.Increment(ref Failed);
            await Console.Error.WriteLineAsync("[" + Done.ToString(CultureInfo.InvariantCulture) + "] " + Pad + (P.ExitCode == 0 ? " ok" : " fail"));
        }
        finally { Sem.Release(); }
    }));
}
await Task.WhenAll(Tasks);
await Console.Error.WriteLineAsync("interactive-captures: done=" + Done.ToString(CultureInfo.InvariantCulture) + " failed=" + Failed.ToString(CultureInfo.InvariantCulture));
return Failed > 0 ? 4 : 0;
