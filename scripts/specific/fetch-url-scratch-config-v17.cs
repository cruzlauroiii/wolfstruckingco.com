return 0;

#:include SharedSpecifics.cs

namespace Scripts
{
    internal static class FetchUrlScratchConfigV17
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("app-index-body", "/wolfstruckingco.com/app/index.html", "body", "", "GET", 1),
        ];
    }
}
