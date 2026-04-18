return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV179
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\pipeline-scene-config.cs";

        public const string Find_01 = "    public const int End = 1;";
        public const string Replace_01 = "    public const int End = 121;";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
