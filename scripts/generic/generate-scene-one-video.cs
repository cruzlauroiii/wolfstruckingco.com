#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string n, string d = "") { var m = Regex.Match(spec, @"const\s+string\s+" + n + @"\s*=\s*@?""(?<v>[^""]*)"""); return m.Success ? m.Groups["v"].Value : d; }

var main = Get("Main");
var frames = Get("Frames");
var audio = Get("Audio");
var docs = Get("Docs");
var scenes = Get("ScenesPath");
var url = Get("HomeUrl");
var pad = Get("ScenePad", "001");
var site = Get("SiteBaseUrl").TrimEnd('/');
Directory.CreateDirectory(frames);
Directory.CreateDirectory(docs);
var png = Path.Combine(frames, pad + ".png");
var mp3 = Path.Combine(audio, "scene-" + pad + ".mp3");
var mp4 = Path.Combine(docs, "scene-" + pad + ".mp4");
var md = Path.Combine(docs, "scene-" + pad + "-ocr-compare.md");

async Task<int> Run(string exe, params string[] a)
{
    var p = new ProcessStartInfo(exe) { WorkingDirectory = main, RedirectStandardOutput = true, RedirectStandardError = true };
    if (exe.Equals("python", StringComparison.OrdinalIgnoreCase))
    {
        p.Environment["PYTHONWARNINGS"] = "ignore";
        p.Environment["FLAGS_use_onednn"] = "0";
        p.Environment["FLAGS_use_mkldnn"] = "0";
    }
    foreach (var x in a) p.ArgumentList.Add(x);
    using var child = Process.Start(p)!;
    var so = child.StandardOutput.ReadToEndAsync();
    var se = child.StandardError.ReadToEndAsync();
    var wait = child.WaitForExitAsync();
    if (await Task.WhenAny(wait, Task.Delay(300000)) != wait) { child.Kill(true); return 124; }
    var outText = await so;
    var errText = await se;
    if (exe.Equals("python", StringComparison.OrdinalIgnoreCase))
    {
        foreach (var line in outText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            if (line.StartsWith("paddleocr-lines=", StringComparison.Ordinal)) Console.WriteLine(line);
        if (child.ExitCode != 0) Console.Error.Write(errText);
    }
    else
    {
        Console.Write(outText);
        Console.Error.Write(errText);
    }
    return child.ExitCode;
}

var cdpConfig = Path.Combine(main, "scripts", "specific", "generated-scene-one-cdp-config.cs");
await File.WriteAllTextAsync(cdpConfig, $$"""
return 0;
namespace Scripts
{
    internal static class CdpRun
    {
        public const string Command = "scene_one_screenshot";
        public const string Url = "{{url.Replace("\"", "\\\"")}}";
        public const string FilePath = @"{{png}}";
    }
}
""");
var cdp = await Run("dotnet", "run", "scripts\\generic\\chrome-devtools.cs", "scripts\\specific\\generated-scene-one-cdp-config.cs");
if (cdp != 0) return cdp;
if (!File.Exists(mp3)) { Console.Error.WriteLine("Missing audio: " + mp3); return 4; }
var ff = await Run("ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-loop", "1", "-i", png, "-i", mp3, "-c:v", "libx264", "-tune", "stillimage", "-pix_fmt", "yuv420p", "-vf", "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30", "-c:a", "aac", "-b:a", "128k", "-ar", "44100", "-shortest", mp4);
if (ff != 0) return ff;

var ocrOut = Path.Combine(Path.GetTempPath(), "scene-" + pad + "-ocr.txt");
var py = Path.Combine(Path.GetTempPath(), "scene-" + pad + "-ocr.py");
await File.WriteAllTextAsync(py, """
import os, sys
import warnings
from pathlib import Path
warnings.filterwarnings("ignore")
os.environ["FLAGS_use_onednn"]="0"; os.environ["FLAGS_use_mkldnn"]="0"
from paddleocr import PaddleOCR
texts=[]
def collect(o):
    if isinstance(o,str):
        if o.strip(): texts.append(o.strip())
    elif isinstance(o,dict):
        for v in o.values(): collect(v)
    elif isinstance(o,(list,tuple)):
        if len(o)>1 and isinstance(o[1],(list,tuple)) and o[1] and isinstance(o[1][0],str): collect(o[1][0])
        else:
            for v in o: collect(v)
ocr=PaddleOCR(lang='en', ocr_version='PP-OCRv4', use_doc_orientation_classify=False, use_doc_unwarping=False, use_textline_orientation=False)
for page in ocr.predict(input=sys.argv[1]):
    collect(getattr(page,'rec_texts',None))
    j=getattr(page,'json',None)
    collect(j() if callable(j) else j)
Path(sys.argv[2]).write_text("\n".join(dict.fromkeys(texts)), encoding="utf-8")
print("paddleocr-lines="+str(len(dict.fromkeys(texts))))
""");
var ocrExit = await Run("python", py, png, ocrOut);
var ocr = File.Exists(ocrOut) ? await File.ReadAllTextAsync(ocrOut) : "";

string narrative = "";
if (File.Exists(scenes))
{
    foreach (var item in JsonDocument.Parse(await File.ReadAllTextAsync(scenes)).RootElement.EnumerateArray())
    {
        var raw = item.ToString();
        if (!raw.Contains(pad) && !raw.Contains(pad.TrimStart('0'))) continue;
        foreach (var n in new[] { "narration", "narrative", "voiceover", "spoken", "script", "text", "caption", "description" })
            if (item.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String) { narrative = p.GetString() ?? ""; break; }
        if (narrative.Length > 0) break;
    }
}
HashSet<string> Words(string s) => Regex.Matches(s.ToLowerInvariant(), @"[a-z0-9]{3,}").Select(m => m.Value).ToHashSet();
var nw = Words(narrative); var ow = Words(ocr); var shared = nw.Intersect(ow).Count();
var grade = nw.Count == 0 ? 0 : Math.Round(100.0 * shared / nw.Count, 1);
var letter = grade >= 80 ? "A" : grade >= 65 ? "B" : grade >= 50 ? "C" : grade >= 35 ? "D" : "F";
await File.WriteAllTextAsync(md, $"# Scene {pad} OCR And Narration Check\n\nVideo: `{mp4}`\n\nPublic URL: {site}/videos/scene-{pad}.mp4\n\nImage: `{png}`\n\nOCR engine: `PaddlePaddle/PaddleOCR`\n\nOCR exit code: `{ocrExit}`\n\nGrade: `{letter}` ({grade}% narrative word coverage)\n\n## Narrative\n\n{narrative}\n\n## Local OCR Text\n\n{ocr.Trim()}\n\n## Comparison\n\n- Narrative words: {nw.Count}\n- OCR words: {ow.Count}\n- Shared words: {shared}\n");
Console.WriteLine("Scene video: " + mp4);
Console.WriteLine("Scene URL: " + site + "/videos/scene-" + pad + ".mp4");
Console.WriteLine("OCR report: " + md);
Console.WriteLine("Grade: " + letter + " (" + grade + "%)");
return 0;
