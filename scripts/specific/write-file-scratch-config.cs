return 0;

namespace Scripts
{
    internal static class WriteFileScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\pub-exec-delete-mp4s-without-audio-tester-config.cs";
        public const string Content = "return 0;\n\nnamespace Scripts\n{\n    internal static class PubExecDeleteMp4sWithoutAudioTesterConfig\n    {\n        public const string ServerUrl = \"wss://wolfs-execution-4444.asse.devtunnels.ms/\";\n        public const string Target = \"tester\";\n        public const string Action = \"dotnet_run\";\n        public const string Generic = @\"main\\scripts\\generic\\delete-mp4s-without-audio.cs\";\n        public const string Config = @\"main\\scripts\\specific\\delete-mp4s-without-audio-scratch-config.cs\";\n    }\n}\n";
        public const string Mode = "overwrite";
    }
}
