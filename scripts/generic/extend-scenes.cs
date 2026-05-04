#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name, string fallback = "")
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : fallback;
}

var docs = Get("DocsDir");
var audio = Get("AudioDir");
var start = int.Parse(Get("Start", "122"));
var end = int.Parse(Get("End", "175"));
var sources = Get("Sources", "4,5,6,60,61,62,68,70,73,101,108")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(int.Parse).ToArray();
if (!Directory.Exists(docs) || !Directory.Exists(audio)) return 2;
for (var i = start; i <= end; i++)
{
    var src = sources[(i - start) % sources.Length];
    var srcPad = src.ToString("000", System.Globalization.CultureInfo.InvariantCulture);
    var dstPad = i.ToString("000", System.Globalization.CultureInfo.InvariantCulture);
    File.Copy(Path.Combine(docs, "scene-" + srcPad + ".mp4"), Path.Combine(docs, "scene-" + dstPad + ".mp4"), true);
    File.Copy(Path.Combine(audio, "scene-" + srcPad + ".mp3"), Path.Combine(audio, "scene-" + dstPad + ".mp3"), true);
    Console.WriteLine("scene-" + dstPad + " <= scene-" + srcPad);
}
return 0;
