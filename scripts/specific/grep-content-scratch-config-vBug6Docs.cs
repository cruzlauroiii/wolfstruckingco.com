namespace WolfsTruckingCo.Scripts.Specific;

public static class GrepContentScratchConfigVBug6Docs
{
    public const string Pattern = @"location\.replace\('/wolfstruckingco\.com/'\)";
    public const string Root = "main";
    public const string FilePattern = "*.html";
    public const string OutputFile = "main/scripts/specific/.bug6-docs.txt";
}
