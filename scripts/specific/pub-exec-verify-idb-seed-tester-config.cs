return 0;

namespace Scripts
{
    internal static class PubExecVerifyIdbSeedTesterConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\verify-idb-seed.cs";
        public const string Config = @"main\scripts\specific\verify-idb-seed-scratch-config.cs";
    }
}
