return 0;

namespace Scripts
{
    internal static class BulkReplace
    {
        public const string RootDir = @"C:\repo\public\wolfstruckingco.com\main\docs\Map";
        public const string Glob = "*.html";
        public const string Find = "xMidYMid slice";
        public const string Replace = "xMidYMid meet";
    }
}
