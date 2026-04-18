return 0;

namespace Scripts
{
    internal static class CdpVerifySectionConfig
    {
        public const string Needle = "console.cloud.google.com/auth/clients/";
        public const string TargetValue = "https://wolfstruckingco.nbth.workers.dev/oauth/google/callback";
        public const string ExpectedSection = "Authorized redirect URIs";
        public const string OtherSection = "Authorized JavaScript origins";
    }
}
