return 0;

namespace Scripts
{
    internal static class PubExecClearRunfileCacheChromeDevtoolsTesterConfigV1
    {
        public const string ServerUrl = "wss://4bjj18z2-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\clear-runfile-cache.cs";
        public const string Config = @"main\scripts\specific\clear-runfile-cache-chrome-devtools-scratch-config-v1.cs";
    }
}
