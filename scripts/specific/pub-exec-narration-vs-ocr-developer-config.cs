return 0;

namespace Scripts
{
    internal static class PubExecNarrationVsOcrDeveloperConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "developer";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\narration-vs-ocr.cs";
        public const string Config = @"main\scripts\specific\narration-vs-ocr-config.cs";
        public const string Workdir = @"C:\repo\public\wolfstruckingco.com";
    }
}
