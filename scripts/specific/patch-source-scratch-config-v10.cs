return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV10
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\pipeline-scene-config.cs";
        public const string Find_01 = "    public const int Start = 1;\n    public const int End = 121;";
        public const string Replace_01 = "    public const int Start = 4;\n    public const int End = 4;";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
