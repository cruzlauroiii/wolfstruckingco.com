return 0;

namespace Scripts
{
    internal static class SearchLogoffDocsConfig
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs";
        public const string Glob = "*.html";
        public const string Pattern = "Log\\s*off|Log\\s*Off|Sign\\s*In|Sign\\s*in";
        public const string Max = "80";
    }
}
