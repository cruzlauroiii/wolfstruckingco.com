return 0;

namespace Scripts
{
    internal static class PubExecDeletePadsProbe004TesterConfigV1
    {
        public const string ServerUrl = "wss://4bjj18z2-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\delete-pads.cs";
        public const string Config = @"main\scripts\specific\delete-pads-probe004-scratch-config-v1.cs";
    }
}
