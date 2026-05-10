return 0;

namespace Scripts
{
    internal static class PubExecListFailuresDeveloperConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "developer";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\list-failures.cs";
        public const string Config = @"main\scripts\specific\list-failures-config.cs";
        public const string Workdir = @"C:\repo\public\wolfstruckingco.com";
    }
}
