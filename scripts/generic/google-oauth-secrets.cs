#:property TargetFramework=net11.0
return 0;

namespace Scripts
{
    internal static class GoogleOauthSecrets
    {
        public const string Provider = "google";
        public const string ClientIdKey = "GOOGLE_CLIENT_ID";
        public const string ClientSecretKey = "GOOGLE_CLIENT_SECRET";
        public const string SecretsJsonPath = @"C:\Users\user1\AppData\Roaming\Microsoft\UserSecrets\prtask-server-secrets\secrets.json";
        public const string IdJsonKey = "Google:ClientId";
        public const string SecretJsonKey = "Google:ClientSecret";
    }
}
