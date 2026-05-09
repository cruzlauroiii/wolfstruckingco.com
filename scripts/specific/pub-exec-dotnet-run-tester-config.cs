return 0;

namespace Scripts
{
    internal static class PubExecDotnetRunTesterConfig
    {
        public const string ServerUrl = "ws://127.0.0.1:4444/";
        public const string Target = "tester";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\count-files.cs";
        public const string Config = @"main\scripts\specific\count-mp4-config.cs";
        public const string Workdir = @"C:\repo\public\wolfstruckingco.com";
    }
}
