return 0;

namespace Scripts
{
    internal static class PubExecBuildBlazorDeveloperConfig
    {
        public const string ServerUrl = "wss://4bjj18z2-4444.asse.devtunnels.ms/";
        public const string Target = "developer";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\build-blazor.cs";
        public const string Config = @"main\scripts\specific\build-blazor-config.cs";
    }
}
