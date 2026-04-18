namespace WolfsTruckingCo.Scripts.Specific;

public static class FetchUrlScratchConfigVKey1
{
    public const string BaseUrl = "https://cruzlauroiii.github.io";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
        ("Key1ApplyChatHref", "/wolfstruckingco.com/Apply/", "grep", "href=\"Chat\"|href=\"/wolfstruckingco.com/Chat", "GET", 1),
    ];
}
