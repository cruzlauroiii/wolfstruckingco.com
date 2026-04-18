return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor.cs";
        public const string Find_01 = "    private bool Sending { get; set; }\n\n    private bool InCall { get; set; }\n\n    private List<ChatMessage> Live { get; } = [];";
        public const string Replace_01 = "    private bool Sending { get; set; }\n\n    private List<ChatMessage> Live { get; } = [];";
        public const string Find_02 = "    private async Task OnKeyAsync(KeyboardEventArgs E)\n    {\n        if (string.Equals(E.Key, EnterKey, StringComparison.Ordinal) && !E.ShiftKey) { await SendAsync(); }\n    }\n\n    private async Task ToggleCallAsync()\n    {\n        if (InCall) { await Wolfs.EndCallAsync(); InCall = false; return; }\n        var Auth = await Wolfs.AuthGetAsync();\n        var Result = await Wolfs.StartCallAsync(Auth.Role ?? ClientRole, Subject);\n        InCall = string.Equals(Result, CallConnected, StringComparison.Ordinal);\n    }\n}";
        public const string Replace_02 = "    private async Task OnKeyAsync(KeyboardEventArgs E)\n    {\n        if (string.Equals(E.Key, EnterKey, StringComparison.Ordinal) && !E.ShiftKey) { await SendAsync(); }\n    }\n}";
        public const string Find_03 = "___UNUSED_SLOT___";
        public const string Replace_03 = "";
    }
}
