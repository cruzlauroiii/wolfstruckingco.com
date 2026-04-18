namespace WolfsTruckingCo.Scripts.Specific;

public static class FetchUrlScratchConfigVVideoCheck
{
    public const string BaseUrl = "https://cruzlauroiii.github.io";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
        ("HomeHasVideo", "/wolfstruckingco.com/", "grep", "HomeWalkthrough|walkthrough\\.mp4|<video", "GET", 1),
        ("VideoFile", "/wolfstruckingco.com/videos/walkthrough.mp4", "head", "", "HEAD", 1),
    ];
}
