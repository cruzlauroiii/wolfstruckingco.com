return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("app.css head", "/wolfstruckingco.com/css/app.css", "head", "", "GET", 1),
            ("index head", "/wolfstruckingco.com/", "head", "", "GET", 1),
        ];
    }
}
