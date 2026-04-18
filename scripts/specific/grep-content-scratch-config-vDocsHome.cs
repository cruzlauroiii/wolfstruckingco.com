namespace WolfsTruckingCo.Scripts.Specific;

public static class GrepContentScratchConfigVDocsHome
{
    public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src";
    public const string FilePattern = "WolfsInteropService.cs";
    public const string Pattern = "Upload|api/upload|HttpClient|FetchAsync|PostAsync";
    public const string OutputFile = @"main\scripts\specific\interop-grep.txt";
}
