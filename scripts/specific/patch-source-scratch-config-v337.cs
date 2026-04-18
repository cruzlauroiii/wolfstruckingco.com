return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV337
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\patch-html-signout.cs";
        public const string Find_01 = @"location.replace(location.pathname.replace(/[^/]+\\\\/?$/,''));";
        public const string Replace_01 = @"location.replace('/wolfstruckingco.com/');";
    }
}
