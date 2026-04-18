return 0;

#:include SharedSpecifics.cs

namespace Scripts
{
    internal static class FetchUrlScratchConfigV21
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("apply-page-body", "/wolfstruckingco.com/Apply/", "body", "", "GET", 1),
        ];
    }
}
