return 0;

namespace Scripts
{
    internal static class PatchSourcePubExecTimeoutScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\pub-exec.cs";
        public const string Find_01 = "using var Timeout = new CancellationTokenSource(TimeSpan.FromMinutes(15));";
        public const string Replace_01 = "using var Timeout = new CancellationTokenSource(TimeSpan.FromMinutes(120));";
    }
}
