return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV58
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\generate-statics.cs";

        public const string Find_01 = "if (args.Length > 0 && args[0].EndsWith(\".cs\", StringComparison.OrdinalIgnoreCase) && File.Exists(args[0]))\n{\n    args = [\"--in-place\"];\n}";
        public const string Replace_01 = "if (args.Length > 0 && args[0].EndsWith(\".cs\", StringComparison.OrdinalIgnoreCase) && File.Exists(args[0]))\n{\n    var ConfigDir = Path.GetDirectoryName(Path.GetFullPath(args[0]))!;\n    var SpecRepo = Path.GetFullPath(Path.Combine(ConfigDir, \"..\", \"..\"));\n    args = [\"--in-place\", SpecRepo];\n}";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
