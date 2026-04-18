return 0;

namespace Scripts
{
    internal static class GithubOauthSecrets
    {
        public const string Provider = "github";
        public const string ClientIdKey = "GITHUB_CLIENT_ID";
        public const string ClientSecretKey = "GITHUB_CLIENT_SECRET";
        public const string SecretsJsonPath = @"C:\Users\user1\AppData\Roaming\Microsoft\UserSecrets\prtask-server-secrets\secrets.json";
        public const string IdJsonKey = "WolfsTrucking:GitHub:ClientId";
        public const string SecretJsonKey = "WolfsTrucking:GitHub:ClientSecret";
    }
}
