return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV167
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "    Directory.CreateDirectory(OutDir);\n    File.WriteAllText(Path.Combine(OutDir, \"index.html\"), Wrap(ScenePathSlug, Html));";
        public const string Replace_01 = "    if (IsPerSceneRoute)\n    {\n        Directory.CreateDirectory(OutDir);\n        File.WriteAllText(Path.Combine(OutDir, \"index.html\"), Wrap(ScenePathSlug, Html));\n    }";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
