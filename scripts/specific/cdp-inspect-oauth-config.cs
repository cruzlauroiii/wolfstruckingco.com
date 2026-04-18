return 0;

namespace Scripts
{
    internal static class CdpInspectOauthConfig
    {
        public const string Url0Match = "console.cloud.google.com";
        public const string Url1Match = "github.com/settings";
        public const string Url2Match = "entra.microsoft.com";
        public const string Url3Match = "okta.com";
        public const string ExpectedCallbackBase = "https://wolfstruckingco.nbth.workers.dev/oauth/";
        public const string WorkerCallbackSubstring = "wolfstruckingco.nbth.workers.dev/oauth";
        public const string WorkerCallbackJsRegex = "https:\\/\\/wolfstruckingco\\.nbth\\.workers\\.dev\\/oauth\\/[a-z]+\\/callback";
    }
}
