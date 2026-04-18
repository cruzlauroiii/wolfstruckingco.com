return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV207
    {
        public const string TargetFile = "main/scripts/generic/delete-files.cs";

        public const string Find_01 = "#:include SharedScripts.cs\nusing Scripts;";
        public const string Replace_01 = "#:property ExperimentalFileBasedProgramEnableIncludeDirective=true\n#:property TargetFramework=net11.0\n#:include SharedScripts.cs\nusing Scripts;";
    }
}
