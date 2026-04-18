namespace WolfsTruckingCo.Scripts.Specific;

public static class GrepContentScratchConfigVBug6Inspect
{
    public const string Pattern = @"FilePath|Path|args|ReadAllText";
    public const string Root = "main/scripts/generic";
    public const string FilePattern = "dump-file.cs";
    public const string OutputFile = "main/scripts/specific/.bug6-dumpfile-schema.txt";
}
