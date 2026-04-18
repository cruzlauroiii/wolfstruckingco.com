return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV51
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\HomePage.razor";

        public const string Find_01 = "        <a href=\"Apply\" class=\"Btn Ghost\">🚛 Apply to drive</a>";
        public const string Replace_01 = "        <a href=\"Apply\" class=\"Btn\">🚛 Sign in to drive</a>";

        public const string Find_02 = "        <a href=\"Track\" class=\"Btn Ghost\">📍 Track</a>";
        public const string Replace_02 = "        <a href=\"Track\" class=\"Btn\">📍 Track a shipment</a>";
    }
}
