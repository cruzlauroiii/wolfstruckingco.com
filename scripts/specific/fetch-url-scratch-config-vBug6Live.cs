namespace WolfsTruckingCo.Scripts.Specific;

public static class FetchUrlScratchConfigVBug6Live
{
    public const string BaseUrl = "https://cruzlauroiii.github.io";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
        ("Bug6LiveLiteralRedirect", "/wolfstruckingco.com/", "grep", "location.replace\\('/wolfstruckingco.com/'\\)", "GET", 1),
    ];
}
