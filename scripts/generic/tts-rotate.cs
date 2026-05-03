#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;
using System.Text;
using System.Text.Json;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

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
var Engine0Cmd = Read("Engine0Cmd");
var Engine0Name = Read("Engine0Name") ?? "bark";
var Engine1Cmd = Read("Engine1Cmd");
var Engine1Name = Read("Engine1Name") ?? "gpt-sovits";
var Engine2Cmd = Read("Engine2Cmd");
var Engine2Name = Read("Engine2Name") ?? "coqui-xtts";
var Engine3Cmd = Read("Engine3Cmd");
var Engine3Name = Read("Engine3Name") ?? "openvoice";
var Engine4Cmd = Read("Engine4Cmd");
var Engine4Name = Read("Engine4Name") ?? "tortoise";

if (NarrationsPath is null || AudioDir is null || IndexOutputPath is null) return 3;
if (!File.Exists(NarrationsPath)) return 4;
Directory.CreateDirectory(AudioDir);
foreach (var F in Directory.GetFiles(AudioDir, "*.wav")) File.Delete(F);

var Engines = new[] { (Engine0Name, Engine0Cmd), (Engine1Name, Engine1Cmd), (Engine2Name, Engine2Cmd), (Engine3Name, Engine3Cmd), (Engine4Name, Engine4Cmd) };

var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(NarrationsPath));
var Items = Doc.RootElement.EnumerateArray().ToArray();
var IndexBuilder = new StringBuilder();
IndexBuilder.Append("[\n");

for (var I = 0; I < Items.Length; I++)
{
    var Pad = (I + 1).ToString("000", System.Globalization.CultureInfo.InvariantCulture);
    var Narration = Items[I].GetProperty("narration").GetString() ?? "";
    var EngineIdx = I % 5;
    var (EngineName, EngineCmdTemplate) = Engines[EngineIdx];
    var WavPath = Path.Combine(AudioDir, $"{Pad}.wav");

    var Normalized = Narration
        .Replace('\u2014', '-')
        .Replace('\u2013', '-')
        .Replace('\u2018', '\'')
        .Replace('\u2019', '\'')
        .Replace('\u201C', '"')
        .Replace('\u201D', '"')
        .Replace('\u00B7', '.');
    var Sb2 = new StringBuilder();
    foreach (var Ch in Normalized) { if (Ch < 128) Sb2.Append(Ch); }
    Normalized = Sb2.ToString();
    var EscNarration = Normalized.Replace("\"", "\\\"", StringComparison.Ordinal);
    var Ok = false;
    var UsedEngine = EngineName;
    for (var Attempt = 0; Attempt < 5 && !Ok; Attempt++)
    {
        var TryIdx = (EngineIdx + Attempt) % 5;
        var (TryName, TryCmd) = Engines[TryIdx];
        if (string.IsNullOrEmpty(TryCmd)) continue;
        var Parts = TryCmd.Split('|');
        if (Parts.Length < 2) { continue; }
        var Psi = new ProcessStartInfo(Parts[0]) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        for (var Pi = 1; Pi < Parts.Length; Pi++)
        {
            var Arg = Parts[Pi].Replace("{text}", Normalized.Trim(), StringComparison.Ordinal).Replace("{out}", WavPath, StringComparison.Ordinal);
            Psi.ArgumentList.Add(Arg);
        }
        try
        {
            using var P = Process.Start(Psi)!;
            var KeywordRe = new System.Text.RegularExpressions.Regex("\\b(warning|error)\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var Killed = false;
            string? OffendingLine = null;
            async Task StreamReader_(StreamReader Sr)
            {
                string? Ln;
                while ((Ln = await Sr.ReadLineAsync()) is not null)
                {
                    if (KeywordRe.IsMatch(Ln) && !Killed)
                    {
                        Killed = true; OffendingLine = Ln;
                        try { P.Kill(true); } catch { }
                        return;
                    }
                }
            }
            var T1 = StreamReader_(P.StandardOutput);
            var T2 = StreamReader_(P.StandardError);
            await P.WaitForExitAsync();
            await Task.WhenAll(T1, T2);
            if (Killed)
            {
                await Console.Error.WriteLineAsync($"scene {Pad} [{TryName}] warning/error: {OffendingLine?.Trim()}");
                return 8;
            }
            Ok = P.ExitCode == 0 && File.Exists(WavPath);
            UsedEngine = TryName + (Attempt > 0 ? $" (fallback {Attempt})" : "");
        }
        catch { }
    }
    AppendEntry(IndexBuilder, I, Items.Length, Pad, Narration, UsedEngine, WavPath, Ok);
    if (!Ok) { await Console.Error.WriteLineAsync($"scene {Pad} all engines failed"); return 7; }
}
IndexBuilder.Append("]\n");
var OutDir = Path.GetDirectoryName(IndexOutputPath);
if (!string.IsNullOrEmpty(OutDir) && !Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);
await File.WriteAllTextAsync(IndexOutputPath, IndexBuilder.ToString());
return 0;

static void AppendEntry(StringBuilder Sb, int I, int Count, string Pad, string Narration, string Engine, string Wav, bool Ok)
{
    var EscNar = Narration.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    var EscWav = Wav.Replace("\\", "\\\\", StringComparison.Ordinal);
    Sb.Append("  {\"index\":");
    Sb.Append((I + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
    Sb.Append(",\"pad\":\"");
    Sb.Append(Pad);
    Sb.Append("\",\"engine\":\"");
    Sb.Append(Engine);
    Sb.Append("\",\"wav\":\"");
    Sb.Append(EscWav);
    Sb.Append("\",\"narration\":\"");
    Sb.Append(EscNar);
    Sb.Append("\",\"ok\":");
    Sb.Append(Ok ? "true" : "false");
    Sb.Append("}");
    if (I < Count - 1) Sb.Append(",");
    Sb.Append("\n");
}
