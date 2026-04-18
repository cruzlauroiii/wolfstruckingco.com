return 0;

namespace Scripts
{
    internal static class VerifySsoConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("Login: SSO anchors target worker /oauth/", "/Login/", "grep", "wolfstruckingco\\.nbth\\.workers\\.dev/oauth/", "GET", 1),
            ("Login: pre-hydration sso snippet inlined", "/Login/", "grep", "location.search.match", "GET", 1),
            ("Login: snippet redirects to Marketplace", "/Login/", "grep", "location.replace", "GET", 1),
            ("Applicant: prompt allows any account", "/Applicant/", "grep", "any account can apply", "GET", 1),
        ];
    }
}
