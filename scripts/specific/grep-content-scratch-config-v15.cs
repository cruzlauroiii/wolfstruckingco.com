return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV15
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs\Chat";
        public const string FilePattern = "*.html";
        public const string Pattern = @"<div class=.ChatStream";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\chatstream-div.txt";
    }
}
