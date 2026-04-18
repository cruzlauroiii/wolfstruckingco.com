return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV42
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs\Chat";
        public const string Pattern = "id=\"app\"|id=\"ChatAttachInput\"|input type=\"file\"|<label class";
        public const string FilePattern = "index.html";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\chat-regen-grep2.txt";
    }
}
