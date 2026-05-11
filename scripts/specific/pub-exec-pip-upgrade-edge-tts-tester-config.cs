return 0;

namespace Scripts
{
    internal static class PubExecPipUpgradeEdgeTtsTesterConfig
    {
        public const string ServerUrl = "wss://4bjj18z2-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\pip-upgrade.cs";
        public const string Config = @"main\scripts\specific\pip-upgrade-edge-tts-scratch-config.cs";
    }
}
