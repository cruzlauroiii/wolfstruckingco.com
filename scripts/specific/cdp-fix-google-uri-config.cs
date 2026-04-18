return 0;

namespace Scripts
{
    internal static class CdpFixGoogleUriConfig
    {
        public const string Needle = "console.cloud.google.com";
        public const string HeadingRegex = "Authorized\\s+redirect\\s+URIs";
        public const string AddButtonRegex = "add\\s*uri";
    }
}
