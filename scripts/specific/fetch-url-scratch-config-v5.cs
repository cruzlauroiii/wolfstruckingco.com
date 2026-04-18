return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfigV5
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("apply-012", "/wolfstruckingco.com/Apply/012/", "head", "", "GET", 1),
            ("documents-016", "/wolfstruckingco.com/Documents/016/", "head", "", "GET", 1),
            ("dashboard-045", "/wolfstruckingco.com/Dashboard/045/", "head", "", "GET", 1),
            ("map-060", "/wolfstruckingco.com/Map/060/", "head", "", "GET", 1),
            ("track-072", "/wolfstruckingco.com/Track/072/", "head", "", "GET", 1),
            ("admin-040", "/wolfstruckingco.com/Admin/040/", "head", "", "GET", 1),
            ("kpi-107", "/wolfstruckingco.com/Investors/KPI/107/", "head", "", "GET", 1),
        ];
    }
}
