return 0;

namespace Scripts
{
    internal static class SearchLogoffSrcConfig
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src";
        public const string Glob = "*.*";
        public const string Pattern = "Log\\s*off|Log\\s*Off|Sign\\s*In|Sign\\s*in|auth|session|user";
        public const string Max = "160";
    }
}
