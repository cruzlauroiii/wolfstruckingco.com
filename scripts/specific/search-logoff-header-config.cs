return 0;

namespace Scripts
{
    internal static class SearchLogoffHeaderConfig
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main";
        public const string Glob = "*.*";
        public const string Pattern = "Log\\s*off|Log\\s*Off|Sign\\s*In|Sign\\s*in";
        public const string Max = "120";
    }
}
