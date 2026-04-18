return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("index NavSecondary class", "/wolfstruckingco.com/", "grep", "NavSecondary", "GET", 1),
            ("index style tag", "/wolfstruckingco.com/", "grep", "<style", "GET", 1),
            ("index link", "/wolfstruckingco.com/", "grep", "<link", "GET", 1),
            ("index display:none NavSecondary", "/wolfstruckingco.com/", "grep", "NavSecondary.*display", "GET", 1),
        ];
    }
}
