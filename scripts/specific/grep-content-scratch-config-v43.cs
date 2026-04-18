return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV43
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs\Chat";
        public const string Pattern = "ChatInputRow|<button|<label class=\"Btn|<input id=\"ChatAttachInput|</body>|<script src=";
        public const string FilePattern = "index.html";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\chat-regen-grep3.txt";
    }
}
