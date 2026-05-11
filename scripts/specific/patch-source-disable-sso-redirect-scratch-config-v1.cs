return 0;

namespace Scripts
{
    internal static class PatchSourceDisableSsoRedirectScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Services\WolfsJsBootstrap.cs";
        public const string Find_01 = "\"    location.href = base + 'Marketplace/';\" + \"\\n\" +";
        public const string Replace_01 = "\"    /* SSO redirect disabled for capture pipeline */\" + \"\\n\" +";
    }
}
