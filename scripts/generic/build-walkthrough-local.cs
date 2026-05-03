#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Diagnostics;

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

var Repo = Read("Repo");
var DiscoverGeneric = Read("DiscoverGeneric");
var DiscoverConfig = Read("DiscoverConfig");
var CaptureGeneric = Read("CaptureGeneric");
var CaptureConfig = Read("CaptureConfig");
var NarrateGeneric = Read("NarrateGeneric");
var NarrateConfig = Read("NarrateConfig");
var TtsGeneric = Read("TtsGeneric");
var TtsConfig = Read("TtsConfig");
var EncodeGeneric = Read("EncodeGeneric");
var EncodeConfig = Read("EncodeConfig");
var ConcatGeneric = Read("ConcatGeneric");
var ConcatConfig = Read("ConcatConfig");

if (Repo is null || DiscoverGeneric is null || DiscoverConfig is null || CaptureGeneric is null || CaptureConfig is null || NarrateGeneric is null || NarrateConfig is null || TtsGeneric is null || TtsConfig is null || EncodeGeneric is null || EncodeConfig is null || ConcatGeneric is null || ConcatConfig is null) return 3;

var KeywordRe = new System.Text.RegularExpressions.Regex("\\b(warning|error)\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
async Task<int> Step(string Label, string Generic, string Config)
{
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Repo };
    foreach (var A in new[] { "run", Generic, Config }) Psi.ArgumentList.Add(A);
    using var P = Process.Start(Psi)!;
    var Killed = false;
    string? OffendingLine = null;
    async Task StreamLines(StreamReader Sr)
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
    var T1 = StreamLines(P.StandardOutput);
    var T2 = StreamLines(P.StandardError);
    await P.WaitForExitAsync();
    await Task.WhenAll(T1, T2);
    if (Killed) { await Console.Error.WriteLineAsync($"{Label} aborted: {OffendingLine?.Trim()}"); return -2; }
    return P.ExitCode;
}

var E1 = await Step("L1", DiscoverGeneric, DiscoverConfig); if (E1 != 0) return 10;
var E2 = await Step("L2", CaptureGeneric, CaptureConfig); if (E2 != 0) return 11;
var E3 = await Step("L3", NarrateGeneric, NarrateConfig); if (E3 != 0) return 12;
var E4 = await Step("L4", TtsGeneric, TtsConfig); if (E4 != 0) return 13;
var E5 = await Step("L5", EncodeGeneric, EncodeConfig); if (E5 != 0) return 14;
var E6 = await Step("L6", ConcatGeneric, ConcatConfig); if (E6 != 0) return 15;
return 0;
