return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfigV8
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("sentinel-garbage", "/wolfstruckingco.com/Definitely/Not/Real/zzz999/", "head", "", "GET", 1),
            ("chat-004-get", "/wolfstruckingco.com/Chat/004/", "head", "", "GET", 1),
            ("chat-065-get", "/wolfstruckingco.com/Chat/065/", "head", "", "GET", 1),
            ("kpi-107-get", "/wolfstruckingco.com/Investors/KPI/107/", "head", "", "GET", 1),
            ("track-121-get", "/wolfstruckingco.com/Track/121/", "head", "", "GET", 1),
            ("marketplace-120-get", "/wolfstruckingco.com/Marketplace/120/", "head", "", "GET", 1),
            ("sentinel-grep-404", "/wolfstruckingco.com/Definitely/Not/Real/zzz999/", "grep", "(?i)file not found|page.{0,10}not.{0,10}found|There isn.t a GitHub Pages site here", "GET", 1),
            ("chat-004-grep-404", "/wolfstruckingco.com/Chat/004/", "grep", "(?i)file not found|page.{0,10}not.{0,10}found|There isn.t a GitHub Pages site here", "GET", 1),
            ("kpi-107-grep-404", "/wolfstruckingco.com/Investors/KPI/107/", "grep", "(?i)file not found|page.{0,10}not.{0,10}found|There isn.t a GitHub Pages site here", "GET", 1),
            ("track-121-grep-404", "/wolfstruckingco.com/Track/121/", "grep", "(?i)file not found|page.{0,10}not.{0,10}found|There isn.t a GitHub Pages site here", "GET", 1),
            ("marketplace-120-grep-404", "/wolfstruckingco.com/Marketplace/120/", "grep", "(?i)file not found|page.{0,10}not.{0,10}found|There isn.t a GitHub Pages site here", "GET", 1),
        ];
    }
}
