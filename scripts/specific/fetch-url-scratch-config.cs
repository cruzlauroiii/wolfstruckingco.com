return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("scene-001.mp4", "/wolfstruckingco.com/videos/scene-001.mp4", "head", "", "GET", 1),
        ];
    }
}
