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
    "en-US-AnaNeural",
    "en-US-AvaMultilingualNeural",
    "en-US-JennyNeural",
    "en-US-AriaNeural",
    "en-US-MichelleNeural",
    "en-US-EmmaMultilingualNeural",
    "en-US-BrianMultilingualNeural",
    "en-US-AndrewMultilingualNeural",
    "en-US-EricNeural",
    "en-US-GuyNeural",
    "en-US-ChristopherNeural",
    "en-GB-SoniaNeural",
    "en-GB-MaisieNeural",
    "en-AU-NatashaNeural",
    "en-AU-WilliamNeural",
};
const string Rate = "+8%";
const string Pitch = "+50Hz";

var FrameDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "frames");
var AudioDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "audio-edge");
Directory.CreateDirectory(AudioDir);

var ScenesPath = Path.Combine(Repo, "docs", "videos", "scenes-final.json");
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
        "python",
        "-m", "edge_tts",
        "--voice", Voice,
        "--rate", Rate,
        "--pitch", Pitch,
        "--text", Narration,
        "--write-media", Mp3);
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
