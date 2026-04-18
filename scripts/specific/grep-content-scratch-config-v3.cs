return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV3
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI";
        public const string FilePattern = "*.scss";
        public const string Pattern = @"ChatStream|ChatBubble|ChatInputRow|ChatBtnRound|\.Agent\b|\.User\b";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\chat-scss.txt";
    }
}
