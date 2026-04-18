return 0;

namespace Scripts
{
    internal static class InspectSecretsConfig
    {
        public const string Path = @"C:\Users\user1\AppData\Roaming\Microsoft\UserSecrets\prtask-server-secrets\secrets.json";
        public const string Mode = "grep";
        public const string Pattern = "ClientId|ClientSecret|GitHub|Microsoft|Okta";
    }
}
