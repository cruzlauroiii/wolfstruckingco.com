return 0;

namespace Scripts
{
    internal static class PubExecReadOcr082TesterConfig
    {
        public const string ServerUrl = "wss://4bjj18z2-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\read-file.cs";
        public const string Config = @"main\scripts\specific\read-ocr-082-scratch-config.cs";
    }
}
