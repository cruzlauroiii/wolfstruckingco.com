return 0;

namespace Scripts
{
    internal static class PubExecDotnetRunDeveloperCloudConfig
    {
        public const string ServerUrl = "wss://4bjj18z2-4444.asse.devtunnels.ms/";
        public const string Target = "developer";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\count-files.cs";
        public const string Config = @"main\scripts\specific\count-mp4-config.cs";
        public const string Workdir = @"C:\repo\public\wolfstruckingco.com";
    }
}
