return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("Marketplace-buyasync", "/wolfstruckingco.com/Marketplace/", "grep", "BuyAsync|Buy now|class=\"Btn\"", "GET", 1),
        ];
    }
}
