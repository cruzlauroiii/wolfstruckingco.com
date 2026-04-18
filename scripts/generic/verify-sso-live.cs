return 0;

namespace Scripts
{
    internal static class VerifySsoLiveConfig
    {
        public const string BaseUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com";

        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("Login: Google SSO anchor to worker", "/Login/", "grep", "/oauth/google/start", "GET", 1),
            ("Login: GitHub SSO anchor to worker", "/Login/", "grep", "/oauth/github/start", "GET", 1),
            ("Login: Microsoft SSO anchor to worker", "/Login/", "grep", "/oauth/microsoft/start", "GET", 1),
            ("Login: Okta SSO anchor to worker", "/Login/", "grep", "/oauth/okta/start", "GET", 1),
            ("Login: pre-hydration snippet", "/Login/", "grep", "wolfs_session", "GET", 1),
            ("Login: snippet redirects to Marketplace", "/Login/", "grep", "Marketplace/", "GET", 1),
            ("Applicant: prompt allows any account", "/Applicant/", "grep", "any account can apply", "GET", 1),
            ("Applicant: still has SSO snippet", "/Applicant/", "grep", "wolfs_session", "GET", 1),
        ];
    }
}
