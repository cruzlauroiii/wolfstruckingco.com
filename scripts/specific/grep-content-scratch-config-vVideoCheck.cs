namespace WolfsTruckingCo.Scripts.Specific;

public static class GrepContentScratchConfigVVideoCheck
{
    public const string Root = @"C:\Users\user1\.claude\projects\C--repo-public-wolfstruckingco-com\5987ed0c-9d95-42b3-a612-61d153477858\tool-results";
    public const string FilePattern = "b40accqry.txt";
    public const string Pattern = "VideoFile|HomeHasVideo|HomeWalkthrough|walkthrough\\.mp4|<video|content-length|status";
    public const string OutputFile = @"main\scripts\specific\video-check-grep.txt";
}
