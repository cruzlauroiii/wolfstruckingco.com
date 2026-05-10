return 0;

namespace Scripts
{
    internal static class GhApiEnableActionsConfig
    {
        public const string SecretsJsonPath = @"C:\Users\user1\AppData\Roaming\Microsoft\UserSecrets\prtask-server-secrets\secrets.json";
        public const string Path = "/repos/cruzlauroiii/wolfstruckingco.com/actions/permissions";
        public const string Method = "PUT";
        public const string Body = "{\"enabled\":true,\"allowed_actions\":\"all\"}";
    }
}
