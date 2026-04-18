return 0;

namespace Scripts
{
    internal static class OktaOauthSecrets
    {
        public const string Provider = "okta";
        public const string ClientIdKey = "OKTA_CLIENT_ID";
        public const string ClientSecretKey = "OKTA_CLIENT_SECRET";
        public const string SecretsJsonPath = @"C:\Users\user1\AppData\Roaming\Microsoft\UserSecrets\prtask-server-secrets\secrets.json";
        public const string IdJsonKey = "Okta:ClientId";
        public const string SecretJsonKey = "Okta:ClientSecret";
    }
}
