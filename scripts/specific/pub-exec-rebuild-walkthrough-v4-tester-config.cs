return 0;

namespace Scripts
{
    internal static class PubExecRebuildWalkthroughV4TesterConfig
    {
        public const string ServerUrl = "wss://4bjj18z2-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\rebuild-walkthrough-v4.cs";
        public const string Config = @"main\scripts\specific\rebuild-walkthrough-v4-scratch-config.cs";
    }
}
