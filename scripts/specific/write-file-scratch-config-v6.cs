return 0;

namespace Scripts
{
    internal static class WriteFileScratchConfigV6
    {
        public const string TargetFile = "main/scripts/generic/delete-files.cs";
        public const string Mode = "overwrite";
        public const string Content = "#:property ExperimentalFileBasedProgramEnableIncludeDirective=true\n#:property TargetFramework=net11.0\n#:include SharedScripts.cs\nusing Scripts;\n\nif (args.Length < 1) { return 1; }\nvar SpecPath = args[0];\nif (!File.Exists(SpecPath)) { return 2; }\n\nvar Strs = await ScratchConfig.LoadStringsAsync(SpecPath);\n\nfor (var I = 1; I <= 99; I++)\n{\n    var Key = $\"Path_{I.ToString(\"D2\", System.Globalization.CultureInfo.InvariantCulture)}\";\n    if (!Strs.TryGetValue(Key, out var P)) { break; }\n    if (string.IsNullOrEmpty(P) || string.Equals(P, \"___UNUSED_SLOT___\", StringComparison.Ordinal)) { break; }\n    if (File.Exists(P)) { File.Delete(P); }\n}\nreturn 0;\n";
    }
}
