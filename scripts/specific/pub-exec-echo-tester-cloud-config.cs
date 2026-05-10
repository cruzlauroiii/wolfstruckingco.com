return 0;

namespace Scripts
{
    internal static class PubExecEchoTesterCloudConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "echo";
        public const string EchoText = "hello via cloud tunnel";
    }
}
