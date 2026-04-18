return 0;

#:include SharedSpecifics.cs

namespace Scripts
{
    internal static class FetchUrlScratchConfigV14
    {
        public const string BaseUrl = SharedSpecifics.LiveBaseUrl;
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("walkthrough-mp4-head", "/wolfstruckingco.com/videos/walkthrough.mp4", "head", "", "HEAD", 1),
        ];
    }
}
