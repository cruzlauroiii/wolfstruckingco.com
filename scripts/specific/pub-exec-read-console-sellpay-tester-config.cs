return 0;

namespace Scripts
{
    internal static class PubExecReadConsoleSellPayTesterConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\read-console.cs";
        public const string Config = @"main\scripts\specific\read-console-sellpay-scratch-config.cs";
    }
}
