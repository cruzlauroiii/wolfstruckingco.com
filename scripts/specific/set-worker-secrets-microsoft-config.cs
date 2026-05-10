return 0;

namespace Scripts
{
    internal static class SetWorkerSecretsMicrosoftConfig
    {
        public const string Provider = "Microsoft";
        public const string ClientIdKey = "MICROSOFT_CLIENT_ID";
        public const string ClientSecretKey = "MICROSOFT_CLIENT_SECRET";
        public const string SecretsJsonPath = @"C:\Users\user1\AppData\Roaming\Microsoft\UserSecrets\prtask-server-secrets\secrets.json";
        public const string IdJsonKey = "Azure:ClientId";
        public const string SecretJsonKey = "Azure:ClientSecret";
    }
}
