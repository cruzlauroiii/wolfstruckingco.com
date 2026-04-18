return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV8
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs\Chat";
        public const string FilePattern = "*.html";
        public const string Pattern = @"ChatStream|ChatBubble|class=""Agent|class=""User""";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\chat-html-bubbles.txt";
    }
}
