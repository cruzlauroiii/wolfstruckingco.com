return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV63
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\regen-statics.cs";

        public const string Find_01 = "$\"run scripts/generate-statics.cs -- --in-place \\\"{Paths.Repo}\\\"\"";
        public const string Replace_01 = "$\"run scripts/generic/generate-statics.cs -- --in-place \\\"{Paths.Repo}\\\"\"";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
