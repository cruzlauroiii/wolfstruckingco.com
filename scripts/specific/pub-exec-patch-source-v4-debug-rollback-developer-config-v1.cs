return 0;

namespace Scripts
{
    internal static class PubExecPatchSourceV4DebugRollbackDeveloperConfigV1
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "developer";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\patch-source.cs";
        public const string Config = @"main\scripts\specific\patch-source-v4-debug-rollback-scratch-config-v1.cs";
    }
}
