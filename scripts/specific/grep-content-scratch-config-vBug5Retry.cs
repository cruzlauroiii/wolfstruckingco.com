return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfig
    {
        public const string Root = @"C:\Users\user1\AppData\Local\Temp";
        public const string FilePattern = "bug5-retry-index.html";
        public const string Pattern = @"\.Hero\{padding:1rem 18px 2rem";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\bug5-retry-grep.txt";
    }
}
