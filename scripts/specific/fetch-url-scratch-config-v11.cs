return 0;

namespace Scripts
{
    internal static class FetchUrlScratchConfigV11
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io";
        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("login-grep-URLSearchParams", "/wolfstruckingco.com/Login/", "grep", "URLSearchParams", "GET", 1),
            ("login-grep-wolfs-role", "/wolfstruckingco.com/Login/", "grep", "wolfs_role", "GET", 1),
            ("login-grep-localStorage", "/wolfstruckingco.com/Login/", "grep", "localStorage.setItem", "GET", 1),
            ("login-grep-marketplace-target", "/wolfstruckingco.com/Login/", "grep", "Marketplace/", "GET", 1),
            ("login-grep-realSess", "/wolfstruckingco.com/Login/", "grep", "realSess", "GET", 1),
            ("login-grep-location-replace", "/wolfstruckingco.com/Login/", "grep", "location.replace", "GET", 1),
            ("login-body-full", "/wolfstruckingco.com/Login/", "body", "", "GET", 1),
        ];
    }
}
