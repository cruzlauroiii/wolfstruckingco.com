return 0;

namespace Scripts
{
    internal static class PubExecSeedIndexeddbTesterConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\seed-indexeddb.cs";
        public const string Config = @"main\scripts\specific\seed-indexeddb-config.cs";
        public const string Workdir = @"C:\repo\public\wolfstruckingco.com";
    }
}
