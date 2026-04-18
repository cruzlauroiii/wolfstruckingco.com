return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV210
    {
        public const string TargetFile = "main/scripts/generic/delete-files.cs";

        public const string Find_01 = "I.ToString(\\\"D2\\\", System.Globalization";
        public const string Replace_01 = "I.ToString(\"D2\", System.Globalization";

        public const string Find_02 = "string.Equals(P, \\\"___UNUSED_SLOT___\\\", StringComparison";
        public const string Replace_02 = "string.Equals(P, \"___UNUSED_SLOT___\", StringComparison";
    }
}
