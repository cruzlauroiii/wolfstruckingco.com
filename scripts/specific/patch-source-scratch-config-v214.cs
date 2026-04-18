return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV214
    {
        public const string TargetFile = "main/scripts/generic/count-lines.cs";

        public const string Find_01 = "{Path.GetFileName(FilePath)}\");\r\nreturn 0;\r\n\r\n";
        public const string Replace_01 = "{Path.GetFileName(FilePath)}\");\r\nreturn 0;\r\n";
    }
}
