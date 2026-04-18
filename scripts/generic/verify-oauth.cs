return 0;

namespace Scripts
{
    internal static class VerifyOauthConfig
    {
        public const string BaseUrl = "https://wolfstruckingco.nbth.workers.dev";

        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("/oauth/google/start -> accounts.google.com", "/oauth/google/start", "redirect", "accounts\\.google\\.com/o/oauth2/v2/auth", "GET", 0),
            ("/oauth/github/start -> github.com", "/oauth/github/start", "redirect", "github\\.com/login/oauth/authorize", "GET", 0),
            ("/oauth/microsoft/start -> microsoftonline.com", "/oauth/microsoft/start", "redirect", "login\\.microsoftonline\\.com/common/oauth2/v2.0/authorize", "GET", 0),
            ("/oauth/okta/start -> okta.com", "/oauth/okta/start", "redirect", "okta\\.com/oauth2/default/v1/authorize", "GET", 0),
            ("/oauth/unknown/start -> 404 unknown provider", "/oauth/unknown/start", "grep", "unknown provider", "GET", 1),
            ("/health -> ok", "/health", "grep", "ok", "GET", 1),
        ];
    }
}
