return 0;

namespace Scripts
{
    internal static class VerifyGoogleRedirectConfig
    {
        public const string BaseUrl = "https://wolfstruckingco.nbth.workers.dev";

        public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
        [
            ("/oauth/google/start: 302 to accounts.google.com", "/oauth/google/start", "redirect", "^https://accounts\\.google\\.com/o/oauth2/v2/auth\\?", "GET", 0),
        ];
    }
}
