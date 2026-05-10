return 0;

namespace Scripts
{
    internal static class PubExecDeletePadsMass004To212TesterConfigV1
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\delete-pads.cs";
        public const string Config = @"main\scripts\specific\delete-pads-mass-004-212-scratch-config-v1.cs";
    }
}
