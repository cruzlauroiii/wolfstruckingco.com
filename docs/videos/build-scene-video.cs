#:property TargetFramework=net11.0-windows10.0.19041.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include pipeline-scene-config.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
var FrameDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "frames");
var AudioDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "audio");
Directory.CreateDirectory(AudioDir);

var ScenesPath = Path.Combine(Repo, "docs", "videos", "scenes-final.json");
if (!File.Exists(ScenesPath)) { await Console.Error.WriteLineAsync("scenes-final.json missing"); return 1; }
var Scenes = JsonDocument.Parse(File.ReadAllText(ScenesPath)).RootElement.EnumerateArray().ToArray();

var SceneN = Math.Max(1, VideoPipeline.PipelineSceneConfig.Start);
if (SceneN > Scenes.Length) { await Console.Error.WriteLineAsync($"scene {SceneN} > {Scenes.Length}"); return 2; }

var Pad = SceneN.ToString("000");
var Png = Path.Combine(FrameDir, Pad + ".png");
if (!File.Exists(Png)) { await Console.Error.WriteLineAsync($"frame missing: {Png}"); return 3; }

var Narration = Scenes[SceneN - 1].GetProperty("narration").GetString() ?? "";
var Wav = Path.Combine(AudioDir, "scene-" + Pad + ".wav");

using var Synth = new SpeechSynthesizer();
var PreferredFemale = new[] { "Aria", "Jenny", "Zira", "David", "Guy" };
var EnVoices = SpeechSynthesizer.AllVoices.Where(V => V.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase)).ToList();
VoiceInformation? Picked = null;
foreach (var Name in PreferredFemale)
{
    Picked = EnVoices.FirstOrDefault(V => V.DisplayName.Contains(Name, StringComparison.OrdinalIgnoreCase));
    if (Picked is not null) { break; }
}
Picked ??= EnVoices.FirstOrDefault(V => V.Gender == VoiceGender.Female) ?? EnVoices.FirstOrDefault();
if (Picked is not null) { Synth.Voice = Picked; }
Synth.Options.SpeakingRate = 1.0;
Synth.Options.AudioVolume = 1.0;

var SafeText = System.Net.WebUtility.HtmlEncode(Narration);
var Ssml = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
           "<prosody pitch='+0%' rate='1.0'>" + SafeText + "</prosody></speak>";
using (var Stream = await Synth.SynthesizeSsmlToStreamAsync(Ssml))
using (var FileStream = File.Create(Wav))
{
    var Buf = new Windows.Storage.Streams.Buffer((uint)Stream.Size);
    await Stream.ReadAsync(Buf, (uint)Stream.Size, InputStreamOptions.None);
    var Reader = DataReader.FromBuffer(Buf);
    var Bytes = new byte[Buf.Length];
    Reader.ReadBytes(Bytes);
    await FileStream.WriteAsync(Bytes);
}

var Bs = File.ReadAllBytes(Wav);
int ByteRate = BitConverter.ToInt32(Bs, 28);
int DataIdx = -1;
for (int I = 12; I < Bs.Length - 8; I++) { if (Bs[I] == 'd' && Bs[I + 1] == 'a' && Bs[I + 2] == 't' && Bs[I + 3] == 'a') { DataIdx = I + 4; break; } }
int DataSize = DataIdx > 0 ? BitConverter.ToInt32(Bs, DataIdx) : Bs.Length - 44;
double Duration = ByteRate > 0 ? (double)DataSize / ByteRate : 3.0;
Duration = Math.Max(1.0, Duration);

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
