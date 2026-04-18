return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("about anchor", "/wolfstruckingco.com/", "grep", "About</a>", "GET", 1),
            ("services anchor", "/wolfstruckingco.com/", "grep", "Services</a>", "GET", 1),
            ("pricing anchor", "/wolfstruckingco.com/", "grep", "Pricing</a>", "GET", 1),
            ("blazor framework", "/wolfstruckingco.com/", "grep", "_framework", "GET", 1),
        ];
    }
}
