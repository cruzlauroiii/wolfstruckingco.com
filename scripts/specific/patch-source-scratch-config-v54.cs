return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV54
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\HomePage.razor";

        public const string Find_01 = "                   CtaLabel=\"Tell the agent what to ship\" />\n\n    <div class=\"Card HomeCtaCard\">";
        public const string Replace_01 = "                   CtaLabel=\"Tell the agent what to ship\" />\n\n    <div class=\"HomeWalkthrough\">\n        <video controls preload=\"metadata\" poster=\"\" playsinline>\n            <source src=\"videos/walkthrough.mp4\" type=\"video/mp4\" />\n        </video>\n        <p class=\"WalkthroughCaption\">7-minute walkthrough — voice-narrated tour of the platform.</p>\n    </div>\n\n    <div class=\"Card HomeCtaCard\">";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
