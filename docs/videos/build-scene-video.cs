#:property TargetFramework=net11.0-windows10.0.19041.0
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
const string CoquiModel = "tts_models/en/vctk/vits";
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
var FrameDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "frames");
var AudioDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "audio-edge");
Directory.CreateDirectory(AudioDir);

var ScenesPath = Path.Combine(Repo, "docs", "videos", "scenes-final-v2.json");
if (!File.Exists(ScenesPath)) { await Console.Error.WriteLineAsync("scenes-final.json missing"); return 1; }
var Scenes = JsonDocument.Parse(File.ReadAllText(ScenesPath)).RootElement.EnumerateArray().ToArray();

var SceneN = Math.Max(1, VideoPipeline.PipelineSceneConfig.Start);
if (SceneN > Scenes.Length) { await Console.Error.WriteLineAsync($"scene {SceneN} > {Scenes.Length}"); return 2; }

var Pad = SceneN.ToString("000");
var Png = Path.Combine(FrameDir, Pad + ".png");
if (!File.Exists(Png)) { await Console.Error.WriteLineAsync($"frame missing: {Png}"); return 3; }

var Narration = Scenes[SceneN - 1].GetProperty("narration").GetString() ?? "";
var Wav = Path.Combine(AudioDir, "scene-" + Pad + ".mp3");
var Voice = VoiceRotation[(SceneN - 1) % VoiceRotation.Length];

var Tts = new ProcessStartInfo("tts") { RedirectStandardError = true, UseShellExecute = false };
foreach (var Arg in new[] { "--text", Narration, "--model_name", CoquiModel, "--speaker_idx", Voice, "--out_path", Wav }) Tts.ArgumentList.Add(Arg);
using (var TtsProc = Process.Start(Tts)!)
{
    var TtsErr = await TtsProc.StandardError.ReadToEndAsync();
    await TtsProc.WaitForExitAsync();
    if (TtsProc.ExitCode != 0) { await Console.Error.WriteLineAsync(TtsErr); return TtsProc.ExitCode; }
}

double Duration = await ProbeDuration(Wav);

var Out = Path.Combine(Repo, "docs", "videos", "scene-" + Pad + ".mp4");
var Args =
    "-y " +
    "-loop 1 -t " + Duration.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " -i \"" + Png + "\" " +
    "-i \"" + Wav + "\" " +
    "-pix_fmt yuv420p -vf \"fps=30,scale=trunc(iw/2)*2:trunc(ih/2)*2\" -r 30 " +
    "-c:v libx264 -preset medium -crf 22 -movflags +faststart " +
    "-c:a aac -b:a 128k -shortest " +
    "\"" + Out + "\"";

var Psi = new ProcessStartInfo("ffmpeg", Args) { RedirectStandardError = true, UseShellExecute = false };
using var Proc = Process.Start(Psi)!;
var Err = await Proc.StandardError.ReadToEndAsync();
await Proc.WaitForExitAsync();
if (Proc.ExitCode != 0) { await Console.Error.WriteLineAsync(Err); return Proc.ExitCode; }

var Size = new FileInfo(Out).Length / 1024.0;
Console.WriteLine($"scene-{Pad}.mp4 — {Size:F0} KB, {Duration:F1}s — {Out}");
return 0;

static async Task<double> ProbeDuration(string audioPath)
{
    var Psi = new ProcessStartInfo("ffprobe") { RedirectStandardOutput = true, UseShellExecute = false };
    foreach (var Arg in new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=nw=1:nk=1", audioPath }) Psi.ArgumentList.Add(Arg);
    using var Proc = Process.Start(Psi)!;
    var Text = await Proc.StandardOutput.ReadToEndAsync();
    await Proc.WaitForExitAsync();
    return double.TryParse(Text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var Duration) ? Math.Max(1.0, Duration) : 3.0;
}
