return 0;

#:include SharedSpecifics.cs

namespace Scripts
{
    internal static class FetchUrlScratchConfigV16
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("app-index-head", "/wolfstruckingco.com/app/", "head", "", "GET", 1),
            ("blazor-js-head", "/wolfstruckingco.com/app/_framework/blazor.webassembly.js", "head", "", "HEAD", 1),
            ("chat-head", "/wolfstruckingco.com/Chat/", "head", "", "GET", 1),
        ];
    }
}
