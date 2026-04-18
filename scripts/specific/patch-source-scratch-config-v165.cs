return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV165
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\ChatPage.razor";

        public const string Find_01 = "@page \"/Chat\"\n@using SharedUI.Components\n\n<div class=\"Stage ChatStage\">\n    <h1>💬 Chat with Agent</h1>\n\n    @if (ShowSignInPrompt)\n    {\n        <div class=\"AuthWarn SignInPrompt\">\n            🔓 Please <a href=\"Login\">sign in</a> to chat with the agent.\n        </div>\n    }\n    else\n    {\n        <p class=\"ChatLede\">@CurrentVariant.Lede</p>\n        <ChatBox SystemPrompt=\"@CurrentVariant.SystemPrompt\"\n                 Placeholder=\"@CurrentVariant.Placeholder\"\n                 AssistantLabel=\"Agent\"\n                 UserLabel=\"You\"\n                 CallTitle=\"@CurrentVariant.CallTitle\"\n                 AttachTitle=\"@CurrentVariant.AttachTitle\"\n                 Subject=\"@CurrentVariant.Subject\" />\n    }\n</div>\n";
        public const string Replace_01 = "@page \"/Chat\"\n@using SharedUI.Components\n@inject SharedUI.Services.WolfsInteropService Wolfs\n\n<div class=\"Stage ChatStage\">\n    <h1>💬 Chat with Agent</h1>\n\n    @if (ShowSignInPrompt)\n    {\n        <div class=\"AuthWarn SignInPrompt\">\n            🔓 Please <a href=\"Login\">sign in</a> to chat with the agent.\n        </div>\n    }\n    else\n    {\n        <p class=\"ChatLede\">@CurrentVariant.Lede</p>\n        <ChatBox SystemPrompt=\"@CurrentVariant.SystemPrompt\"\n                 Placeholder=\"@CurrentVariant.Placeholder\"\n                 AssistantLabel=\"Agent\"\n                 UserLabel=\"You\"\n                 CallTitle=\"@CurrentVariant.CallTitle\"\n                 AttachTitle=\"@CurrentVariant.AttachTitle\"\n                 Subject=\"@CurrentVariant.Subject\" />\n    }\n</div>\n\n@code {\n    protected override async Task OnAfterRenderAsync(bool firstRender)\n    {\n        if (!firstRender) { return; }\n        var Auth = await Wolfs.AuthGetAsync();\n        var Role = Auth?.Role ?? string.Empty;\n        if (string.IsNullOrEmpty(Role))\n        {\n            ShowSignInPrompt = true;\n            StateHasChanged();\n            return;\n        }\n        var Resolved = ResolveVariantByRole(Role);\n        if (!ReferenceEquals(Resolved, CurrentVariant))\n        {\n            CurrentVariant = Resolved;\n            StateHasChanged();\n        }\n    }\n}\n";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
