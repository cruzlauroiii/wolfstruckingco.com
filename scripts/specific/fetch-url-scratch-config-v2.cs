return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfigV2
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("scene-001", "/wolfstruckingco.com/videos/scene-001.mp4", "head", "", "GET", 1),
            ("scene-060", "/wolfstruckingco.com/videos/scene-060.mp4", "head", "", "GET", 1),
            ("scene-121", "/wolfstruckingco.com/videos/scene-121.mp4", "head", "", "GET", 1),
        ];
    }
}
