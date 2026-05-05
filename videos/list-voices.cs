#:property TargetFramework=net11.0-windows10.0.19041.0
#:property RunAnalyzersDuringBuild=false

using Windows.Media.SpeechSynthesis;
foreach (var V in SpeechSynthesizer.AllVoices.Where(V => V.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase)))
{
    Console.WriteLine($"{V.DisplayName} ({V.Language}, {V.Gender})");
}
