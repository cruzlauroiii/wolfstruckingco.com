return 0;

namespace Scripts
{
    internal static class PubExecKillChromeTesterConfigV1
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\kill-by-name.cs";
        public const string Config = @"main\scripts\specific\kill-by-name-chrome-scratch-config-v1.cs";
    }
}
