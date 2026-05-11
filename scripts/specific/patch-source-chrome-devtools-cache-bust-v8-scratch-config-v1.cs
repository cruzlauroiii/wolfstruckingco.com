return 0;

namespace Scripts
{
    internal static class PatchSourceChromeDevtoolsCacheBustV8ScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\chrome-devtools.cs";
        public const string Find_01 = "// cache-bust v7 close-all-non-chrome-fresh";
        public const string Replace_01 = "// cache-bust v8 reverted-close-all (use newtab-only)";
    }
}
