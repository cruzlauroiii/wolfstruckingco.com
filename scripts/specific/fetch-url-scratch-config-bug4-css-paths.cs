return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("app.css alt1", "/wolfstruckingco.com/app.css", "head", "", "GET", 1),
            ("app.css alt2", "/wolfstruckingco.com/_content/SharedUI/css/app.css", "head", "", "GET", 1),
            ("app.css alt3", "/wolfstruckingco.com/scss/app.css", "head", "", "GET", 1),
            ("index grep css href", "/wolfstruckingco.com/", "grep", "\\.css", "GET", 1),
        ];
    }
}
