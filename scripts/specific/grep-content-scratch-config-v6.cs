return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV6
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs\videos";
        public const string FilePattern = "*.cs";
        public const string Pattern = @"AskAi|claude-opus|anthropic|api\.anthropic|model\s*=";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\askai-refs.txt";
    }
}
