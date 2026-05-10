return 0;

namespace Scripts
{
    internal static class PubExecPatchSourceCdpCommandsFromSurfaceDeveloperConfigV1
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "developer";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\patch-source.cs";
        public const string Config = @"main\scripts\specific\patch-source-cdpcommands-fromsurface-scratch-config-v1.cs";
    }
}
