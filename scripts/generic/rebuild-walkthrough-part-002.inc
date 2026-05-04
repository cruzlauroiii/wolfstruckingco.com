    while (DateTime.UtcNow < Deadline)
    {
        if (File.Exists(SsoAckPath)) { try { File.Delete(SsoAckPath); } catch { } Console.WriteLine($"  *** SSO ack scene {Pad}"); return; }
        AlarmBurst();
        for (int I = 0; I < 20; I++) { if (File.Exists(SsoAckPath)) { try { File.Delete(SsoAckPath); } catch { } Console.WriteLine($"  *** SSO ack scene {Pad}"); return; } await Task.Delay(100); }
    }
    var FinalUrl = await CurrentUrl();
    Console.WriteLine($"  *** SSO hard timeout scene {Pad}, url={FinalUrl} -- ABORT");
    Environment.Exit(2);
}

string PadFor(JsonElement Scene, int Idx)
{
    var T = Scene.GetProperty("target").GetString() ?? "";
    if (T.Contains("cb=")) { var Cb = T.Split("cb=")[^1].Replace("?", "").Replace("/", "").Trim(); return Cb.Substring(0, Math.Min(3, Cb.Length)); }
    return Idx.ToString("D3");
}

using var GroupsDoc = JsonDocument.Parse(ChatGroupsJson);
var GroupsRoot = GroupsDoc.RootElement;
(string?, Dictionary<string, string>?) FindGroup(string Pad)
{
    foreach (var Kv in GroupsRoot.EnumerateObject())
    {
        var Pads = Kv.Value.GetProperty("pads").EnumerateArray().Select(e => e.GetString()).ToList();
        if (Pads.Contains(Pad))
        {
            var Msgs = Kv.Value.GetProperty("msgs").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
            return (Kv.Name, Msgs);
        }
    }
    return (null, null);
}

var ScenesText = await File.ReadAllTextAsync(ScenesPath);
var Scenes = JsonDocument.Parse(ScenesText).RootElement;

var MissingAudio = new List<string>();
foreach (var Sc in Scenes.EnumerateArray())
{
    var P = PadFor(Sc, 0);
    var W = Path.Combine(Audio, $"scene-{P}.mp3");
    if (!File.Exists(W) || new FileInfo(W).Length == 0) MissingAudio.Add(P);
}
if (MissingAudio.Count > 0)
{
    Console.WriteLine($"PRECONDITION FAIL: missing/empty audio wav for {MissingAudio.Count} scenes in {Audio}");
    Console.WriteLine($"  pads: {string.Join(",", MissingAudio)}");
    return 4;
}

Console.WriteLine("chrome-devtools probe skipped; relying on auto-spawn serve + reuse-active-tab in chrome-devtools.cs new_page.");

Console.WriteLine($"starting scene loop. total scenes={Scenes.GetArrayLength()}");

string? CurrentGroup = null;
int OkCount = 0;
var FailPads = new List<(int idx, JsonElement scene, string pad)>();

async Task<(bool ok, string? group, string pad)> RunScene(int Idx, JsonElement Scene, string? CurrentGroupIn)
{
    var Pad = PadFor(Scene, Idx);
    var Target = Scene.GetProperty("target").GetString() ?? "";
    var IsChat = Target.Contains("/Chat/");
    var IsSso = Scene.TryGetProperty("sso", out var SsoEl) && !string.IsNullOrEmpty(SsoEl.GetString());
    var (GroupKey, Group) = IsChat ? FindGroup(Pad) : (null, null);
    var Cg = CurrentGroupIn;
    try
    {
        if (GroupKey is not null && GroupKey != Cg)
        {
            await ReplaceTab(Target); await Task.Delay(5000);
            await ForceLight();
            Cg = GroupKey;
        }
        else if (GroupKey is not null && GroupKey == Cg) { await ForceLight(); }
        else
        {
            await ReplaceTab(Target); await Task.Delay(4000);
            if (IsSso) await WaitForSso(SsoEl.GetString() ?? "", Scene.TryGetProperty("account", out var Ac) ? Ac.GetString() ?? "" : "", Pad);
            await ForceLight();
            Cg = null;
        }
        if (IsChat && Group is not null && Group.TryGetValue(Pad, out var Msg)) { await SendMsg(Msg); await Task.Delay(15000); await ForceLight(); }
        if (Pad == "001")
        {
            if (await HasLogoffText())
            {
                await ClickLogoffLink(); await Task.Delay(6000);
                await ClearStorageAndReload(); await Task.Delay(5000);
                await GotoHome(); await Task.Delay(6000);
                await ForceLight();
            }
            if (await HasLogoffText()) { Console.WriteLine($"  *** scene 001 STILL shows Log off -- ABORT"); Environment.Exit(3); }
        }
        await Screenshot(Pad);
        var Enc = await Encode(Pad);
        Console.WriteLine($"  {Pad} {(IsChat ? "chat" : "nav")} grp={GroupKey} sso={IsSso} enc={Enc}");
        return (Enc == 0, Cg, Pad);
    }
    catch (Exception E) { Console.WriteLine($"  {Pad} EXC {E.Message}"); return (false, Cg, Pad); }
}

int Idx0 = 0;
foreach (var Scene in Scenes.EnumerateArray())
{
    Idx0++;
    var (Ok, Cg, Pad) = await RunScene(Idx0, Scene, CurrentGroup);
    CurrentGroup = Cg;
    if (!Ok)
    {
        Console.WriteLine($"  *** scene {Pad} FAILED -- ABORT (idx={Idx0})");
        return 5;
    }
    OkCount++;
}
Console.WriteLine($"PASS1 ok={OkCount}");
var StillFail = new List<string>();

int ExpectedScenes = Scenes.GetArrayLength();
var MissingMp4 = new List<string>();
foreach (var Scene in Scenes.EnumerateArray())
{
    var P = PadFor(Scene, 0);
    var M = Path.Combine(Docs, $"scene-{P}.mp4");
    if (!File.Exists(M) || new FileInfo(M).Length == 0) MissingMp4.Add(P);
}

var Mp4s = Directory.GetFiles(Docs, "scene-*.mp4").OrderBy(f => f).ToList();
Console.WriteLine($"concat candidates: {Mp4s.Count}");
var ConcatTxt = Path.Combine(Path.GetTempPath(), "concat-v2.txt");
await File.WriteAllLinesAsync(ConcatTxt, Mp4s.Select(m => $"file '{m.Replace("\\", "/")}'"));
var Walk = Path.Combine(Docs, "walkthrough.mp4");
try { File.Delete(Walk); } catch { }
var ConcatPsi = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var A in new[] { "-y", "-f", "concat", "-safe", "0", "-i", ConcatTxt, "-c", "copy", Walk }) ConcatPsi.ArgumentList.Add(A);
using var Cp = Process.Start(ConcatPsi)!;
var ConcatOut = Cp.StandardOutput.ReadToEndAsync();
var ConcatErr = Cp.StandardError.ReadToEndAsync();
await Task.WhenAll(ConcatOut, ConcatErr, Cp.WaitForExitAsync());
Console.WriteLine($"concat rc={Cp.ExitCode} -> {Walk}");

int Failures = StillFail.Count + MissingMp4.Count + (Cp.ExitCode != 0 ? 1 : 0) + (!File.Exists(Walk) || new FileInfo(Walk).Length == 0 ? 1 : 0);
if (Failures > 0)
{
    Console.WriteLine($"FAIL: stillFail={StillFail.Count} missingMp4={MissingMp4.Count} concatRc={Cp.ExitCode} walkOk={(File.Exists(Walk) && new FileInfo(Walk).Length > 0)}");
    Console.WriteLine($"  stillFail pads: {string.Join(",", StillFail)}");
    Console.WriteLine($"  missingMp4 pads: {string.Join(",", MissingMp4)}");
    return 1;
}
Console.WriteLine($"DONE all {ExpectedScenes} scenes encoded, walkthrough.mp4 ok");
return 0;
