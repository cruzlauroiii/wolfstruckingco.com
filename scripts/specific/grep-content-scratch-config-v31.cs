namespace Scripts;

internal static class GrepContentScratchConfigV31
{
    public const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs\videos";
    public const string Pattern = @"Console\.(WriteLine|Write|Out\.|WriteAsync|WriteLineAsync)";
    public const string FilePattern = "run-crud-pipeline.cs";
    public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-output-v31.txt";
}
