return 0;

namespace Scripts
{
    internal static class CdpCreateGithubOauthAppConfig
    {
        public const string FormUrl = "https://github.com/settings/applications/new";
        public const string PageNeedle = "github.com/settings/applications/new";
        public const string AppName = "Wolfs Trucking Co";
        public const string HomepageUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com/";
        public const string AppDescription = "Wolfs Trucking Co web app SSO via Cloudflare Worker";
        public const string CallbackUrl = "https://wolfstruckingco.nbth.workers.dev/oauth/github/callback";
        public const string SubmitButtonText = "Register application";
    }
}
