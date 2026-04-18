return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV209
    {
        public const string TargetFile = "main/scripts/generic/delete-files.cs";

        public const string Find_01 = "}\r\nreturn 0;\r\n\r\n";
        public const string Replace_01 = "}\r\nreturn 0;\r\n";
    }
}
