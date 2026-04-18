return 0;

namespace Scripts
{
    internal static class WriteFileScratchConfigV2
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\DispatcherPage.razor";
        public const string Content = "@page \"/Dispatcher\"\n@using SharedUI.Components\n\n<div class=\"Stage ChatStage DispatcherStage\">\n    <h1>📡 Dispatcher</h1>\n    <p class=\"ChatLede\">@ChatVariantConstants.DispatcherLede</p>\n    <ChatBox SystemPrompt=\"@ChatVariantConstants.DispatcherSystemPrompt\"\n             Placeholder=\"@ChatVariantConstants.DispatcherPlaceholder\"\n             AssistantLabel=\"Dispatcher\"\n             UserLabel=\"Driver\"\n             CallTitle=\"@ChatVariantConstants.DispatcherCallTitle\"\n             AttachTitle=\"@ChatVariantConstants.DispatcherAttachTitle\"\n             Subject=\"@ChatVariantConstants.DispatcherSubject\" />\n</div>\n";
        public const string Mode = "overwrite";
    }
}
