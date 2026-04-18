return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("Marketplace BuyNow anchor", "/wolfstruckingco.com/Marketplace/", "grep", "/wolfstruckingco\\.com/Buy/ShipTo/", "GET", 1),
        ];
    }
}
