#:property TargetFramework=net11.0
return 0;

namespace Scripts
{
    internal static class GrepFindGoogleOauth
    {
        public const string Path = @"C:\Users\user1\AppData\Roaming\Microsoft\UserSecrets\prtask-server-secrets\secrets.json";
        public const string Pattern = "google.*(client|oauth)|oauth.*google";
    }
}
