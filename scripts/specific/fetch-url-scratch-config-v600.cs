public static class FetchUrlScratchConfigV600
{
    public const string BaseUrl = "https://cruzlauroiii.github.io";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
    [
        ("marketplace-shipto", "/wolfstruckingco.com/Marketplace/", "grep", "/wolfstruckingco\\.com/Buy/ShipTo/", "GET", 1),
    ];
}
