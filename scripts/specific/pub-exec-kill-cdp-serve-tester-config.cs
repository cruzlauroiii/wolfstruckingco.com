return 0;

namespace Scripts
{
    internal static class PubExecKillCdpServeTesterConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\kill-cdp-serve.cs";
        public const string Config = @"main\scripts\specific\delete-pads-scratch-config.cs";
    }
}
