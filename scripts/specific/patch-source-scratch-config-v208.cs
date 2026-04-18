return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV208
    {
        public const string TargetFile = "main/scripts/generic/delete-files.cs";

        public const string Find_01 = "}\nreturn 0;\n\n";
        public const string Replace_01 = "}\nreturn 0;\n";
    }
}
