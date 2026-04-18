namespace Scripts;

internal static class PatchSourceScratchConfigV223
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\concat-walkthrough.cs";

    public const string Find_01 = "if (P.ExitCode != 0) { await Console.Error.WriteLineAsync(Err); return P.ExitCode; }\n\nvar Size = new FileInfo(Output).Length / 1024.0 / 1024.0;\nConsole.WriteLine($\"walkthrough.mp4 — {Size:F1} MB at {Output}\");\nreturn 0;";
    public const string Replace_01 = "if (P.ExitCode != 0) { await Console.Error.WriteLineAsync(Err); return P.ExitCode; }\nreturn 0;";
}
