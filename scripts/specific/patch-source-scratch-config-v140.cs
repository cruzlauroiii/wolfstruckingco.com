return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV140
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes-final.json";

        public const string Find_01 = "wolfstruckingco.com/Dispatcher/?cb=083";
        public const string Replace_01 = "wolfstruckingco.com/Chat/?cb=083";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
