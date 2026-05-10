return 0;

namespace Scripts
{
    internal static class PatchSourceChromeDevtoolsCacheBustScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\chrome-devtools.cs";
        public const string Find_01 = "#:include CdpCommands.cs";
        public const string Replace_01 = "// cache-bust v1\n#:include CdpCommands.cs";
    }
}
