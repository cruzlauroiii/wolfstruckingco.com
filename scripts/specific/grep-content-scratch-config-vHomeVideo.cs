namespace WolfsTruckingCo.Scripts.Specific;

public static class GrepContentScratchConfigVHomeVideo
{
    public const string Root = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific";
    public const string FilePattern = "home-live.html";
    public const string Pattern = "walkthrough\\.mp4|HomeWalkthrough|<video|<source";
    public const string OutputFile = @"main\scripts\specific\home-video-grep.txt";
}
