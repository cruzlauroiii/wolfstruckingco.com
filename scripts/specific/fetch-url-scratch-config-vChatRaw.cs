namespace WolfsTruckingCo.Scripts.Specific;

public static class FetchUrlScratchConfigVChatRaw
{
    public const string BaseUrl = "https://cruzlauroiii.github.io";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
        ("ChatRaw", "/wolfstruckingco.com/Chat/", "save", "main/scripts/specific/chat-live.html", "GET", 1),
    ];
}
