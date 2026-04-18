return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV32
    {
        public const string Pattern = "WolfsInteropService|WolfsJsBootstrap|WolfsInterop\\.|EnsureInstalledAsync";
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src";
        public const string FilePattern = "*.cs";
        public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-out-v32.txt";
    }
}
