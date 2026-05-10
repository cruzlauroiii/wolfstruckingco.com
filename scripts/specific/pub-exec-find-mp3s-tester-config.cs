return 0;

namespace Scripts
{
    internal static class PubExecFindMp3sTesterConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\find-mp3s.cs";
        public const string Config = @"main\scripts\specific\find-mp3s-scratch-config.cs";
    }
}
