return 0;

namespace Scripts
{
    internal static class PatchWorkerCallbackConfig
    {
        public const string WorkerCs = @"C:\repo\public\wolfstruckingco.com\main\worker\worker.cs";
        public const string Find = "location.replace(\\'https://cruzlauroiii.github.io/wolfstruckingco.com/Marketplace/\\');";
        public const string Replace = "location.replace(\\'https://cruzlauroiii.github.io/wolfstruckingco.com/Login/?sso=\\' + provider + \\'&email=\\' + encodeURIComponent(email) + \\'&session=\\' + encodeURIComponent(session));";
    }
}
