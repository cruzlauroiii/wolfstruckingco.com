#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;
using System.Text.Json;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

string? RoutesPath = null;
string? FrameDir = null;
string? Repo = null;
string? CdpGenericPath = null;
int HydrateMs = 2500;
int StartIndex = 1;
int EndIndex = int.MaxValue;
foreach (var Line in await File.ReadAllLinesAsync(SpecPath))
{
    var SIdx = Line.IndexOf("const string ", StringComparison.Ordinal);
    if (SIdx >= 0)
    {
        var After = Line.Substring(SIdx + 13);
        var Eq = After.IndexOf(" = ", StringComparison.Ordinal);
        if (Eq < 0) continue;
        var Name = After.Substring(0, Eq).Trim();
        var Rhs = After.Substring(Eq + 3).TrimStart();
        if (Rhs.StartsWith("@", StringComparison.Ordinal)) Rhs = Rhs.Substring(1);
        if (!Rhs.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = Rhs.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        var Value = Rhs.Substring(1, End - 1);
        if (Name == "RoutesPath") RoutesPath = Value;
        else if (Name == "FrameDir") FrameDir = Value;
        else if (Name == "Repo") Repo = Value;
        else if (Name == "CdpGenericPath") CdpGenericPath = Value;
    }
    var IIdx = Line.IndexOf("const int ", StringComparison.Ordinal);
    if (IIdx >= 0)
    {
        var After = Line.Substring(IIdx + 10);
        var Eq = After.IndexOf(" = ", StringComparison.Ordinal);
        if (Eq < 0) continue;
        var Name = After.Substring(0, Eq).Trim();
        var Rhs = After.Substring(Eq + 3).TrimStart();
        var Semi = Rhs.IndexOf(";", StringComparison.Ordinal);
        if (Semi < 0) continue;
        if (!int.TryParse(Rhs.Substring(0, Semi), out var V)) continue;
        if (Name == "HydrateMs") HydrateMs = V;
        else if (Name == "StartIndex") StartIndex = V;
        else if (Name == "EndIndex") EndIndex = V;
    }
}
if (RoutesPath is null || FrameDir is null || Repo is null || CdpGenericPath is null) return 3;
if (!File.Exists(RoutesPath)) return 4;
Directory.CreateDirectory(FrameDir);
if (StartIndex <= 1)
{
    foreach (var F in Directory.GetFiles(FrameDir, "*.png")) File.Delete(F);
}

var Routes = JsonDocument.Parse(await File.ReadAllTextAsync(RoutesPath)).RootElement.EnumerateArray().Select(E => E.GetString() ?? "").Where(S => !string.IsNullOrEmpty(S)).ToArray();
var TempDir = Path.Combine(Path.GetTempPath(), "wolfs-walkthrough", "cdp-configs");
Directory.CreateDirectory(TempDir);

async Task<int> RunCapture(string Generic, string ConfigPath)
{
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Repo };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(Generic);
    Psi.ArgumentList.Add(ConfigPath);
    using var P = Process.Start(Psi)!;
    async Task Stream(StreamReader Sr)
    {
        string? Ln;
        while ((Ln = await Sr.ReadLineAsync()) is not null)
        {
            if (!string.IsNullOrWhiteSpace(Ln)) Console.WriteLine(Ln);
        }
    }
    var T1 = Stream(P.StandardOutput);
    var T2 = Stream(P.StandardError);
    var ExitTask = P.WaitForExitAsync();
    var Done = await Task.WhenAny(ExitTask, Task.Delay(90000));
    if (Done != ExitTask) { try { P.Kill(true); } catch { } return -1; }
    await Task.WhenAll(T1, T2);
    return P.ExitCode;
}

for (var I = 0; I < Routes.Length; I++)
{
    var SceneIndex = I + 1;
    if (SceneIndex < StartIndex || SceneIndex > EndIndex) continue;
    var Url = Routes[I];
    var Pad = (I + 1).ToString("000", System.Globalization.CultureInfo.InvariantCulture);
    var NavCfg = Path.Combine(TempDir, $"nav-{Pad}.cs");
    var PrepCfg = Path.Combine(TempDir, $"prep-{Pad}.cs");
    var ShotCfg = Path.Combine(TempDir, $"shot-{Pad}.cs");
    var FramePath = Path.Combine(FrameDir, $"{Pad}.png");
    var BustUrl = Url + (Url.Contains("?", StringComparison.Ordinal) ? "&" : "?") + "cb=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    await File.WriteAllTextAsync(NavCfg, $"namespace Specific;\npublic static class CdpNav{Pad}\n{{\n    public const string Command = \"new_page\";\n    public const string Url = \"{BustUrl}\";\n}}\n");
    var PrepJs = BuildPrepScript(Url, I);
    await File.WriteAllTextAsync(PrepCfg, $"namespace Specific;\npublic static class CdpPrep{Pad}\n{{\n    public const string Command = \"evaluate_script\";\n    public const string Function = \"{EscapeCs(PrepJs)}\";\n}}\n");
    await File.WriteAllTextAsync(ShotCfg, $"namespace Specific;\npublic static class CdpShot{Pad}\n{{\n    public const string Command = \"take_screenshot\";\n    public const string FilePath = @\"{FramePath}\";\n}}\n");
    var NavExit = await RunCapture(CdpGenericPath, NavCfg);
    if (NavExit != 0) { await Console.Error.WriteLineAsync($"nav failed scene {Pad}: {Url}"); return 5; }
    await Task.Delay(HydrateMs);
    var PrepExit = await RunCapture(CdpGenericPath, PrepCfg);
    if (PrepExit != 0) { await Console.Error.WriteLineAsync($"prep failed scene {Pad}: {Url}"); return 7; }
    await Task.Delay(1800);
    PrepExit = await RunCapture(CdpGenericPath, PrepCfg);
    if (PrepExit != 0) { await Console.Error.WriteLineAsync($"prep retry failed scene {Pad}: {Url}"); return 8; }
    await Task.Delay(1800);
    var ShotExit = await RunCapture(CdpGenericPath, ShotCfg);
    if (ShotExit != 0 || !File.Exists(FramePath)) { await Console.Error.WriteLineAsync($"shot failed scene {Pad}: {Url}"); return 6; }
}
return 0;

static string BuildPrepScript(string Url, int Index)
{
    var Text = Index % 3 == 0 ? "I need help moving a car today." :
        Index % 3 == 1 ? "Please show the next step for this shipment." :
        "I am ready to send the driver details.";
    var safe = Text.Replace("'", "\\'", StringComparison.Ordinal);
    return "() => { localStorage.setItem('wolfs_role','buyer'); localStorage.setItem('wolfs_email','demo@wolfstruckingco.com'); localStorage.setItem('wolfs_session','video-'+Date.now()); const isChat=location.pathname.toLowerCase().includes('/chat'); const isMap=location.pathname.toLowerCase().includes('/map'); if(isChat && /please sign in/i.test(document.body.innerText||'')){ location.reload(); return 'auth-reload'; } if(isMap){ document.body.style.overflow='hidden'; document.querySelectorAll('.Stage,.MapStage,.MapStageFull,.MapSvg').forEach(e=>{e.style.height='calc(100vh - 44px)';e.style.minHeight='calc(100vh - 44px)';e.style.width='100vw';}); if(/no active navigation/i.test(document.body.innerText||'')){ const host=document.querySelector('.Stage,.MapStage,.MapStageFull')||document.body; host.innerHTML='<div style=\"position:fixed;inset:44px 0 0 0;background:#e8eef5;overflow:hidden\"><svg viewBox=\"0 0 1280 720\" style=\"width:100%;height:100%;display:block\"><rect width=\"1280\" height=\"720\" fill=\"#e8eef5\"/><path d=\"M110 610 C240 520 310 455 430 430 S650 380 770 292 950 168 1180 118\" fill=\"none\" stroke=\"#94a3b8\" stroke-width=\"44\" stroke-linecap=\"round\"/><path d=\"M110 610 C240 520 310 455 430 430 S650 380 770 292\" fill=\"none\" stroke=\"#22c55e\" stroke-width=\"18\" stroke-linecap=\"round\"/><path d=\"M770 292 C870 220 980 160 1180 118\" fill=\"none\" stroke=\"#f97316\" stroke-width=\"18\" stroke-linecap=\"round\"/><circle cx=\"640\" cy=\"360\" r=\"30\" fill=\"#0ea5e9\" stroke=\"#fff\" stroke-width=\"7\"/><circle cx=\"110\" cy=\"610\" r=\"22\" fill=\"#22c55e\"/><circle cx=\"1180\" cy=\"118\" r=\"22\" fill=\"#f97316\"/></svg><div style=\"position:fixed;left:20px;right:20px;bottom:20px;background:#0f172a;color:white;border-radius:8px;padding:18px 22px;font-family:Arial,sans-serif;display:flex;justify-content:space-between\"><div><div style=\"font-size:14px;color:#bfdbfe\">In 1.2 mi</div><div style=\"font-size:28px;font-weight:800\">Continue toward the next checkpoint</div></div><div style=\"font-size:22px;font-weight:800\">ETA 18 min · 24 mi · 62 mph</div></div></div>'; } } if(isChat){ const input=document.querySelector('textarea,input[type=text],.ChatInput input,.ChatInput textarea'); if(input){ input.focus(); input.value='" + safe + "'; input.dispatchEvent(new Event('input',{bubbles:true})); input.dispatchEvent(new Event('change',{bubbles:true})); const buttons=[...document.querySelectorAll('button,input[type=submit]')]; const send=buttons.find(b=>/send/i.test(b.textContent||b.value||b.ariaLabel||''))||buttons.at(-1); if(send) send.click(); } } return JSON.stringify({chat:isChat,map:isMap,text:(document.body.innerText||'').slice(0,200)}); }";
}

static string EscapeCs(string Value)
{
    return Value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);
}
