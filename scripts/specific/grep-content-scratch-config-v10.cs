return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV10
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs\Chat";
        public const string FilePattern = "*.html";
        public const string Pattern = @"<div class=""ChatStream|class=""ChatBubble";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\chat-bubbles-only.txt";
    }
}
