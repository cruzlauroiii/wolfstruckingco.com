return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor.cs";
        public const string Find_01 = "    private const string EnterKey = \"Enter\";\n    private const string CallConnected = \"connected\";\n    private const string ClientRole = \"client\";\n    private const string DefaultSystemPrompt = \"You are Wolfs Trucking dispatcher. Reply briefly.\";";
        public const string Replace_01 = "    private const string EnterKey = \"Enter\";\n    private const string DefaultSystemPrompt = \"You are Wolfs Trucking dispatcher. Reply briefly.\";";
        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
