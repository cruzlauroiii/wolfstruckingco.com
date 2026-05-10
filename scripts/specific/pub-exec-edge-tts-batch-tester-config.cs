return 0;

namespace Scripts
{
    internal static class PubExecEdgeTtsBatchTesterConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\edge-tts-batch.cs";
        public const string Config = @"main\scripts\specific\edge-tts-batch-scratch-config.cs";
    }
}
