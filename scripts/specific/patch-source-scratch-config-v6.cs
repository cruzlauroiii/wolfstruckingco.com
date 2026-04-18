return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV6
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor.cs";
        public const string Find_01 = "    private List<ChatMessage> Live { get; } = [];\n\n    private static Task AttachAsync() => Task.CompletedTask;";
        public const string Replace_01 = "    private List<ChatMessage> Live { get; } = [];\n\n    protected override void OnInitialized()\n    {\n        Live.Clear();\n        foreach (var Turn in WolfsRenderContext.ChatHistory)\n        {\n            var Role = string.Equals(Turn.Role, \"agent\", StringComparison.OrdinalIgnoreCase) ? RoleAssistant : RoleUser;\n            Live.Add(new ChatMessage(Role, Turn.Text));\n        }\n    }\n\n    private static Task AttachAsync() => Task.CompletedTask;";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
