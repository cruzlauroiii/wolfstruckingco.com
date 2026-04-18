namespace WolfsTruckingCo.Scripts.Specific;

public static class FetchUrlScratchConfigVHomeRaw
{
    public const string BaseUrl = "https://cruzlauroiii.github.io";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
        ("HomeRaw", "/wolfstruckingco.com/", "save", "main/scripts/specific/home-live.html", "GET", 1),
    ];
}
