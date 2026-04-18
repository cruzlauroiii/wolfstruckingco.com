return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV212
    {
        public const string TargetFile = "main/scripts/generic/count-lines.cs";

        public const string Find_01 = "return 0;\n\n";
        public const string Replace_01 = "return 0;\n";
    }
}
