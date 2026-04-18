#:property TargetFramework=net11.0-windows10.0.19041.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// build-video.cs — assemble walkthrough.mp4 from the PNG frames captured by
// run-crud-pipeline.cs, with synced TTS narration. Each scene's narration is
// synthesized via Windows.Media.SpeechSynthesis (offline neural voice), the
// PNG is held for the synth duration, and the per-scene WAVs are concatenated
// into a single audio track muxed into the final MP4.
//
//   dotnet run docs/videos/build-video.cs

using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
var FrameDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "frames");
var AudioDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "audio");
Directory.CreateDirectory(AudioDir);
foreach (var F in Directory.GetFiles(AudioDir)) { File.Delete(F); }

var ScenesPath = Path.Combine(Repo, "docs", "videos", "scenes-final.json");
var Out = Path.Combine(Repo, "docs", "videos", "walkthrough.mp4");
var ConcatVideo = Path.Combine(FrameDir, "concat.txt");
var ConcatAudio = Path.Combine(AudioDir, "concat.txt");

if (!Directory.Exists(FrameDir)) { Console.Error.WriteLine($"frames missing: {FrameDir}"); return 1; }
var Scenes = JsonDocument.Parse(File.ReadAllText(ScenesPath)).RootElement.EnumerateArray().ToArray();

// Anime-girl style voice: prefer Microsoft Ana (child neural voice) which is
// the highest-pitched cute en-US voice on Windows and most-cited match for
// anime-girl narration. Fall back through other youthful cheerful voices,
// then Aria. Pitch raised via SSML so the output stays cute even on Aria.
using var Synth = new SpeechSynthesizer();
var PreferredFemale = new[] { "Ana", "Ashley", "Sara", "Sonia", "Jane", "Aria", "Jenny", "Emma", "Ava" };
var EnVoices = SpeechSynthesizer.AllVoices.Where(V => V.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase)).ToList();
VoiceInformation? Picked = null;
foreach (var Name in PreferredFemale)
{
    Picked = EnVoices.FirstOrDefault(V => V.DisplayName.Contains(Name, StringComparison.OrdinalIgnoreCase));
    if (Picked is not null) { break; }
}
Picked ??= EnVoices.FirstOrDefault(V => V.Gender == VoiceGender.Female) ?? EnVoices.FirstOrDefault();
if (Picked is not null) { Synth.Voice = Picked; Console.WriteLine($"voice: {Picked.DisplayName} ({Picked.Language}, {Picked.Gender})"); }
// Rate handled in SSML so duration calc on the produced WAV stays accurate.
Synth.Options.SpeakingRate = 1.0;
Synth.Options.AudioVolume = 1.0;
// Only Microsoft Zira ships locally on Windows 11 SAPI 5 (David, Zira, Mark).
// Aria/Ana/Jenny are Narrator-only and not reachable via Windows.Media.SpeechSynthesis.
// Push Zira's pitch up substantially so the output reads as cute/anime-girl-ish.
const string SsmlPitch = "+35%";

var Vb = new StringBuilder();
var Ab = new StringBuilder();
double Total = 0;
for (int N = 1; N <= Scenes.Length; N++)
{
    var Pad = N.ToString("000");
    var Png = Path.Combine(FrameDir, Pad + ".png");
    if (!File.Exists(Png)) { Console.Error.WriteLine($"missing frame {Pad}"); continue; }
    var Narration = Scenes[N - 1].GetProperty("narration").GetString() ?? "";

    // Synthesize narration → WAV via SSML so pitch can be raised.
    var Wav = Path.Combine(AudioDir, Pad + ".wav");
    var SafeText = System.Net.WebUtility.HtmlEncode(Narration);
    var Voice = Picked?.DisplayName ?? "";
    var Ssml = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
               "<prosody pitch='" + SsmlPitch + "' rate='1.05'>" + SafeText + "</prosody></speak>";
    using (var Stream = await Synth.SynthesizeSsmlToStreamAsync(Ssml))
    using (var FileStream = File.Create(Wav))
    {
        var Buffer = new Windows.Storage.Streams.Buffer((uint)Stream.Size);
        await Stream.ReadAsync(Buffer, (uint)Stream.Size, InputStreamOptions.None);
        var Reader = DataReader.FromBuffer(Buffer);
        var Bytes = new byte[Buffer.Length];
        Reader.ReadBytes(Bytes);
        await FileStream.WriteAsync(Bytes);
    }

    // Measure the produced WAV (PCM 16k mono) — duration = byteCount / byteRate.
    // Read RIFF header to extract byte rate (offset 28) and data size (after "data" chunk).
    var Bs = File.ReadAllBytes(Wav);
    int ByteRate = BitConverter.ToInt32(Bs, 28);
    int DataIdx = -1;
    for (int I = 12; I < Bs.Length - 8; I++)
    {
        if (Bs[I] == 'd' && Bs[I + 1] == 'a' && Bs[I + 2] == 't' && Bs[I + 3] == 'a') { DataIdx = I + 4; break; }
    }
    int DataSize = DataIdx > 0 ? BitConverter.ToInt32(Bs, DataIdx) : Bs.Length - 44;
    double Duration = ByteRate > 0 ? (double)DataSize / ByteRate : 3.0;
    Duration = Math.Max(1.0, Duration);  // exact wav duration — keeps audio + video perfectly synced across all scenes

    Vb.AppendLine($"file '{Png.Replace('\\', '/')}'");
    Vb.AppendLine($"duration {Duration:F2}");
    Ab.AppendLine($"file '{Wav.Replace('\\', '/')}'");
    Total += Duration;

    if (N % 20 == 0 || N == Scenes.Length) { Console.WriteLine($"  ✓ {Pad}/{Scenes.Length} synth — running {Total:F1}s"); }
}
// ffmpeg concat demuxer requires the last frame entry to be repeated without duration.
var Last = Path.Combine(FrameDir, Scenes.Length.ToString("000") + ".png");
Vb.AppendLine($"file '{Last.Replace('\\', '/')}'");
File.WriteAllText(ConcatVideo, Vb.ToString());
File.WriteAllText(ConcatAudio, Ab.ToString());

Console.WriteLine($"\nconcat: {Scenes.Length} frames, total {Total:F1}s = {Total / 60:F2} min");
Console.WriteLine($"running ffmpeg → {Out}");

var Args = "-y -f concat -safe 0 -i " + Q(ConcatVideo) +
           " -f concat -safe 0 -i " + Q(ConcatAudio) +
           " -pix_fmt yuv420p -vf \"fps=30,scale=trunc(iw/2)*2:trunc(ih/2)*2\" -r 30 " +
           "-c:v libx264 -preset medium -crf 22 -movflags +faststart " +
           "-c:a aac -b:a 128k -shortest " +
           Q(Out);
var Psi = new ProcessStartInfo("ffmpeg", Args) { RedirectStandardError = true, UseShellExecute = false };
using var Proc = Process.Start(Psi)!;
var Err = await Proc.StandardError.ReadToEndAsync();
await Proc.WaitForExitAsync();
if (Proc.ExitCode != 0) { Console.Error.WriteLine(Err); return Proc.ExitCode; }

var Size = new FileInfo(Out).Length / 1024.0 / 1024.0;
Console.WriteLine($"\n✓ walkthrough.mp4 — {Size:F1} MB, {Total:F1}s ({Total / 60:F2} min) with TTS narration");
Console.WriteLine($"  open: {Out}");
return 0;

static string Q(string Path) => "\"" + Path + "\"";
