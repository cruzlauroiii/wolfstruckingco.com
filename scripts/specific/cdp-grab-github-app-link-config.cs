return 0;

namespace Scripts
{
    internal static class CdpGrabGithubAppLinkConfig
    {
        public const string Url = "https://github.com/settings/developers?type=oauth-app";
        public const string AppLinkPattern = "\\/settings\\/applications\\/\\d+";
    }
}
