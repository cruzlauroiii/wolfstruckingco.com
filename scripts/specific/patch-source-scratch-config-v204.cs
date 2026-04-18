return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV204
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\launch-chrome.cs";

        public const string Find_01 = "var Arg3 = StringFromConfig(\"Arg3\", \"--disable-blink-features=AutomationControlled\");";
        public const string Replace_01 = "var Arg3 = StringFromConfig(\"Arg3\", \"\");";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
