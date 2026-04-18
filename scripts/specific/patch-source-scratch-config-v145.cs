return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV145
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes.cs";

        public const string Find_01 = "Add(\"/Schedule/\",      \"System recomputes downstream legs from live traffic.\");\nAdd(\"/Dispatcher/\",    \"Agent tells the buyer the new ETA.\");";
        public const string Replace_01 = "Add(\"/Dispatcher/\",    \"Agent tells the buyer the new ETA.\");\nAdd(\"/Schedule/\",      \"System recomputes downstream legs from live traffic.\");";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
