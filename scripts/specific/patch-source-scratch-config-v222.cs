namespace Scripts;

internal static class PatchSourceScratchConfigV222
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\build-all-scenes.cs";

    public const string Find_01 = "    if (N % 10 == 0 || N == 1 || N == Scenes.Length)\n    {\n        var Size = new FileInfo(Mp4).Length / 1024.0;\n        Console.WriteLine($\"  ✓ scene-{Pad}.mp4 — {Size:F0} KB\");\n    }\n}";
    public const string Replace_01 = "}";

    public const string Find_02 = "if (Failures > 0) { await Console.Error.WriteLineAsync($\"failures: {Failures.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"); return 6; }\nConsole.WriteLine($\"done — {Scenes.Length} scenes built\");\nreturn 0;";
    public const string Replace_02 = "if (Failures > 0) { await Console.Error.WriteLineAsync($\"failures: {Failures.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"); return 6; }\nreturn 0;";
}
