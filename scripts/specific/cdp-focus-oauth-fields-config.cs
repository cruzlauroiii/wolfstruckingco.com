return 0;

namespace Scripts
{
    internal static class CdpFocusOauthFieldsConfig
    {
        public const string P1Label = "google";
        public const string P1Needle = "console.cloud.google.com";
        public const string P1Callback = "https://wolfstruckingco.nbth.workers.dev/oauth/google/callback";
        public const string P1Hint = "paste in Authorized redirect URIs";
        public const string P1NavUrl = "";

        public const string P2Label = "github";
        public const string P2Needle = "github.com/settings";
        public const string P2Callback = "https://wolfstruckingco.nbth.workers.dev/oauth/github/callback";
        public const string P2Hint = "navigated to OAuth App settings - paste in Authorization callback URL";
        public const string P2NavUrl = "https://github.com/settings/applications/Ov23liBXCLsiuJ664RcV";

        public const string P3Label = "microsoft";
        public const string P3Needle = "entra.microsoft.com";
        public const string P3Callback = "https://wolfstruckingco.nbth.workers.dev/oauth/microsoft/callback";
        public const string P3Hint = "navigated to Authentication blade - Add platform - Web - paste";
        public const string P3NavUrl = "https://entra.microsoft.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Authentication/appId/c5ed7649-d57b-4023-840a-aa1e3e4a7a2d";

        public const string P4Label = "okta";
        public const string P4Needle = "okta.com";
        public const string P4Callback = "https://wolfstruckingco.nbth.workers.dev/oauth/okta/callback";
        public const string P4Hint = "click Edit on General Settings, scroll to Sign-in redirect URIs, paste";
        public const string P4NavUrl = "";
    }
}
