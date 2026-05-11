return 0;

namespace Scripts
{
    internal static class PubExecGitRunDeveloperConfig
    {
        public const string ServerUrl = "wss://4bjj18z2-4444.asse.devtunnels.ms/";
        public const string Target = "developer";
        public const string Action = "dotnet_run";
        public const string Generic = @"main\scripts\generic\git-run.cs";
        public const string Config = @"main\scripts\specific\git-run-scratch-config.cs";
    }
}
