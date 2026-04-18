return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV213
    {
        public const string TargetFile = "main/scripts/generic/count-lines.cs";

        public const string Find_01 = "{Path.GetFileName(FilePath)}\");\nreturn 0;";
        public const string Replace_01 = "{Path.GetFileName(FilePath)}\");\nreturn 0;\n";
    }
}
