return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfig
    {
        public const string BaseUrl = "http://localhost:8080";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("home-cb003-light", "/?cb=003&theme=light", "head", "", "GET", 1),
        ];
    }
}
