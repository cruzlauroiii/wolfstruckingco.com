return 0;

namespace Scripts
{
    internal static class TunnelLoginConfig
    {
        public const string SecretsJsonPath = @"C:\Users\user1\AppData\Roaming\Microsoft\UserSecrets\prtask-server-secrets\secrets.json";
        public const string TenantKey = "Azure:TenantId";
        public const string ClientIdKey = "Azure:ClientId";
        public const string ClientSecretKey = "Azure:ClientSecret";
        public const string DefaultTenant = "9188040d-6c67-4c5b-b112-36a304b66dad";
        public const string DevtunnelExe = @"C:\Users\user1\AppData\Local\Microsoft\WinGet\Links\devtunnel.exe";
    }
}
