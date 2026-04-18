return 0;

namespace Scripts
{
    internal static class PatchWorkerOktaDomainConfig
    {
        public const string WorkerCs = @"C:\repo\public\wolfstruckingco.com\main\worker\worker.cs";
        public const string Find = "example.okta.com";
        public const string Replace = "integrator-8035923.okta.com";
    }
}
