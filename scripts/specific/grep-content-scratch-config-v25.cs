return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV25
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\scripts";
        public const string FilePattern = "*.cs";
        public const string Pattern = @"\bScratchPatterns\b|\bScratchConfig\b\s*\.";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\shared-name-collision-check.txt";
    }
}
