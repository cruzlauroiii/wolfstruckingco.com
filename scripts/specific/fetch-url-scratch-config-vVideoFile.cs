namespace WolfsTruckingCo.Scripts.Specific;

public static class FetchUrlScratchConfigVVideoFile
{
    public const string BaseUrl = "https://cruzlauroiii.github.io";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
        ("VideoHead", "/wolfstruckingco.com/videos/walkthrough.mp4", "head", "", "HEAD", 1),
    ];
}
