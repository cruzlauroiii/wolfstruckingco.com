return 0;

namespace Scripts
{
    internal static class PatchSourceChatpageTrailingSlashScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\ChatPage.razor";
        public const string Find_01 = "@page \"/Chat\"";
        public const string Replace_01 = "@page \"/Chat\"\n@page \"/Chat/\"";
    }
}
