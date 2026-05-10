return 0;

namespace Scripts
{
    internal static class PubExecDeleteMp4sWithoutAudioTesterConfig
    {
        public const string ServerUrl = "wss://wolfs-execution-4444.asse.devtunnels.ms/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\delete-mp4s-without-audio.cs";
        public const string Config = @"main\scripts\specific\delete-mp4s-without-audio-scratch-config.cs";
    }
}
