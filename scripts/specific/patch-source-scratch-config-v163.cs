return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV163
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\ChatPage.razor";

        public const string Find_01 = "@page \"/Chat\"\n@using System.Security.Claims\n@using Microsoft.AspNetCore.Components.Authorization\n@using SharedUI.Components\n\n<div class=\"Stage ChatStage\">\n    <h1>💬 Chat with Agent</h1>\n\n    <AuthorizeView Context=\"Auth\">\n        <Authorized>\n            @{ var Variant = ResolveVariant(Auth.User); }\n            <p class=\"ChatLede\">@Variant.Lede</p>\n            <ChatBox SystemPrompt=\"@Variant.SystemPrompt\"\n                     Placeholder=\"@Variant.Placeholder\"\n                     AssistantLabel=\"Agent\"\n                     UserLabel=\"You\"\n                     CallTitle=\"@Variant.CallTitle\"\n                     AttachTitle=\"@Variant.AttachTitle\"\n                     Subject=\"@Variant.Subject\" />\n        </Authorized>\n        <NotAuthorized>\n            <div class=\"AuthWarn SignInPrompt\">\n                🔓 Please <a href=\"Login\">sign in</a> to chat with the agent.\n            </div>\n        </NotAuthorized>\n    </AuthorizeView>\n</div>\n";
        public const string Replace_01 = "@page \"/Chat\"\n@using SharedUI.Components\n\n<div class=\"Stage ChatStage\">\n    <h1>💬 Chat with Agent</h1>\n\n    @if (ShowSignInPrompt)\n    {\n        <div class=\"AuthWarn SignInPrompt\">\n            🔓 Please <a href=\"Login\">sign in</a> to chat with the agent.\n        </div>\n    }\n    else\n    {\n        <p class=\"ChatLede\">@CurrentVariant.Lede</p>\n        <ChatBox SystemPrompt=\"@CurrentVariant.SystemPrompt\"\n                 Placeholder=\"@CurrentVariant.Placeholder\"\n                 AssistantLabel=\"Agent\"\n                 UserLabel=\"You\"\n                 CallTitle=\"@CurrentVariant.CallTitle\"\n                 AttachTitle=\"@CurrentVariant.AttachTitle\"\n                 Subject=\"@CurrentVariant.Subject\" />\n    }\n</div>\n";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
