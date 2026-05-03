#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include pipeline-scene-config.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
var VoiceRotation = new[]
{
    "p225", "p226", "p227", "p228", "p229", "p230", "p231", "p232", "p233", "p234",
    "p236", "p237", "p238", "p239", "p240", "p241", "p243", "p244", "p245", "p246",
    "p247", "p248", "p249", "p250", "p251", "p252", "p253", "p254", "p255", "p256",
    "p257", "p258", "p259", "p260", "p261", "p262", "p263", "p264", "p265", "p266",
    "p267", "p268", "p269", "p270", "p271", "p272", "p273", "p274", "p275", "p276",
    "p277", "p278", "p279", "p280", "p281", "p282", "p283", "p284", "p285", "p286",
    "p287", "p288", "p292", "p293", "p294", "p295", "p297", "p298", "p299", "p300",
    "p301", "p302", "p303", "p304", "p305", "p306", "p307", "p308", "p310", "p311",
    "p312", "p313", "p314", "p316", "p317", "p318", "p323", "p326", "p329", "p330",
    "p333", "p334", "p335", "p336", "p339", "p340", "p341", "p343", "p345", "p347",
    "p351", "p360", "p361", "p362", "p363", "p364", "p374", "p376",
};
const string CoquiModel = "tts_models/en/vctk/vits";

var FrameDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "frames");
var AudioDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "audio-edge");
Directory.CreateDirectory(AudioDir);

var ScenesPath = Path.Combine(Repo, "docs", "videos", "scenes-final-v2.json");
if (!File.Exists(ScenesPath)) { await Console.Error.WriteLineAsync("scenes-final.json missing"); return 1; }
var Scenes = JsonDocument.Parse(await File.ReadAllTextAsync(ScenesPath)).RootElement.EnumerateArray().ToArray();

async Task<int> RunAsync(string Exe, params string[] Args)
{
    var Psi = new ProcessStartInfo(Exe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var A in Args) { Psi.ArgumentList.Add(A); }
    using var P = Process.Start(Psi)!;
    _ = await P.StandardOutput.ReadToEndAsync();
    var Err = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    if (P.ExitCode != 0) { await Console.Error.WriteLineAsync(Exe + ": " + Err.Trim()); }
    return P.ExitCode;
}

var Failures = 0;
var SceneStart = Math.Max(1, VideoPipeline.PipelineSceneConfig.Start);
var SceneEnd = Math.Min(Scenes.Length, VideoPipeline.PipelineSceneConfig.End);
for (var N = SceneStart; N <= SceneEnd; N++)
{
    var Pad = N.ToString("000");
    var Png = Path.Combine(FrameDir, Pad + ".png");
    if (!File.Exists(Png)) { await Console.Error.WriteLineAsync("missing frame " + Pad); Failures++; continue; }

    var Narration = Scenes[N - 1].GetProperty("narration").GetString() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(Narration)) { Narration = "Scene " + Pad; }

    var Mp3 = Path.Combine(AudioDir, "scene-" + Pad + ".mp3");
    var Voice = VoiceRotation[(N - 1) % VoiceRotation.Length];
    var TtsExit = await RunAsync(
        "tts",
        "--text", Narration,
        "--model_name", CoquiModel,
        "--speaker_idx", Voice,
        "--out_path", Mp3);
    if (TtsExit != 0) { Failures++; continue; }

    var Mp4 = Path.Combine(Repo, "docs", "videos", "scene-" + Pad + ".mp4");
    var FfExit = await RunAsync(
        "ffmpeg",
        "-y",
        "-loop", "1",
        "-framerate", "30",
        "-i", Png,
        "-i", Mp3,
        "-c:v", "libx264",
        "-tune", "stillimage",
        "-pix_fmt", "yuv420p",
        "-preset", "medium",
        "-crf", "22",
        "-g", "30",
        "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2",
        "-c:a", "aac",
        "-b:a", "192k",
        "-movflags", "+faststart",
        "-shortest",
        Mp4);
    if (FfExit != 0) { Failures++; continue; }

}

if (Failures > 0) { await Console.Error.WriteLineAsync($"failures: {Failures.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); return 6; }
return 0;
