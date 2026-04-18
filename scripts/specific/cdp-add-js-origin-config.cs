return 0;

namespace Scripts
{
    internal static class CdpAddJsOriginConfig
    {
        public const string Needle = "console.cloud.google.com/auth/clients/";
        public const string SectionHeading = "Authorized JavaScript origins";
        public const string ValueToAdd = "https://wolfstruckingco.nbth.workers.dev";
    }
}
