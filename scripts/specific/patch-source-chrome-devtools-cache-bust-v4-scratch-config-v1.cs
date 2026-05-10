return 0;

namespace Scripts
{
    internal static class PatchSourceChromeDevtoolsCacheBustV4ScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\chrome-devtools.cs";
        public const string Find_01 = "// cache-bust v3 newtab-close-and-readywait";
        public const string Replace_01 = "// cache-bust v4 strict-newtab-url";
    }
}
