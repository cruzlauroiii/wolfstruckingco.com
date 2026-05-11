return 0;

namespace Scripts
{
    internal static class PubExecProbeSnapshotChatTesterConfigV1
    {
        public const string ServerUrl = "wss://4bjj18z2-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\probe-snapshot.cs";
        public const string Config = @"main\scripts\specific\probe-snapshot-chat-scratch-config-v1.cs";
    }
}
