#:property TargetFramework=net11.0-windows
#:package System.Speech@9.0.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property BuiltInComInteropSupport=true
#:property PublishAot=false
using System.Speech.Synthesis;
using System.Text.Json;
using System.Text.Json.Nodes;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;

var Specs = await File.ReadAllLinesAsync(SpecPath);
string? Read(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        if (After.StartsWith("@", StringComparison.Ordinal)) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}

var NarrationsPath = Read("NarrationsPath");
var AudioDir = Read("AudioDir");
var IndexOutputPath = Read("IndexOutputPath");
if (NarrationsPath is null || AudioDir is null || IndexOutputPath is null) return 3;
if (!File.Exists(NarrationsPath)) return 4;
Directory.CreateDirectory(AudioDir);

var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(NarrationsPath));
var Voices = new[] { "Microsoft Zira Desktop", "Microsoft David Desktop", "Microsoft Hazel Desktop", "Microsoft Mark Desktop" };
var Index = new JsonArray();
var I = 0;
JsonElement[] Items;
if (Doc.RootElement.ValueKind == JsonValueKind.Array)
{
    Items = Doc.RootElement.EnumerateArray().ToArray();
}
else
{
    Items = Doc.RootElement.EnumerateObject().Select(P =>
    {
        var Wrap = JsonNode.Parse($"{{\"pad\":\"{P.Name}\",\"narration\":{System.Text.Json.JsonSerializer.Serialize(P.Value.GetString() ?? string.Empty)}}}")!;
        return JsonDocument.Parse(Wrap.ToJsonString()).RootElement;
    }).ToArray();
}
foreach (var Item in Items)
{
    var Pad = Item.TryGetProperty("pad", out var PadEl) ? PadEl.GetString() ?? (I + 1).ToString("000") : (I + 1).ToString("000");
    var Text = Item.TryGetProperty("narration", out var NarEl) ? NarEl.GetString() ?? "" : "";
    var Plain = new System.Text.StringBuilder();
    foreach (var Ch in Text)
    {
        if (Ch == '—' || Ch == '–') Plain.Append(' ');
        else if (Ch == '•') Plain.Append(' ');
        else if (Ch == '‘' || Ch == '’') Plain.Append('\'');
        else if (Ch == '“' || Ch == '”') Plain.Append('"');
        else if (Ch < 32) Plain.Append(' ');
        else if (Ch > 127) continue;
        else Plain.Append(Ch);
    }
    var Cleaned = Plain.ToString().Trim();
    if (string.IsNullOrEmpty(Cleaned)) Cleaned = "scene " + Pad;

    var OutPath = Path.Combine(AudioDir, $"{Pad}.wav");
    using (var Synth = new SpeechSynthesizer())
    {
        try { Synth.SelectVoice(Voices[I % Voices.Length]); } catch { }
        Synth.Rate = 0;
        Synth.Volume = 100;
        Synth.SetOutputToWaveFile(OutPath);
        Synth.Speak(Cleaned);
    }
    var Entry = new JsonObject { ["pad"] = Pad, ["audio"] = OutPath, ["audioPath"] = OutPath, ["engine"] = Voices[I % Voices.Length], ["ok"] = true };
    Index.Add(Entry);
    I++;
}
await File.WriteAllTextAsync(IndexOutputPath, Index.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
return 0;
