return 0;

namespace Scripts
{
    internal static class PubExecDeleteFilesStaleOcrTesterConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\delete-files.cs";
        public const string Config = @"main\scripts\specific\delete-files-stale-ocr-scratch-config.cs";
    }
}
