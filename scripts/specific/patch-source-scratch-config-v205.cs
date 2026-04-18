return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV205
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\launch-chrome-config.cs";

        public const string Find_01 = "        public const string Arg3 = \"--disable-blink-features=AutomationControlled\";";
        public const string Replace_01 = "        public const string Arg3 = \"\";";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
