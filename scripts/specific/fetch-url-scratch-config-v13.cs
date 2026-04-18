return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfigV13
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("walkthrough-mp4-head", "/wolfstruckingco.com/videos/walkthrough.mp4", "head", "", "HEAD", 1),
            ("dispatcher-page-head", "/wolfstruckingco.com/Dispatcher/", "head", "", "HEAD", 1),
        ];
    }
}
