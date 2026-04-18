return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV206
    {
        public const string TargetFile = "main/scripts/generic/delete-files.cs";

        public const string Find_01 = "using System.Text.RegularExpressions;\nusing Scripts;\n\nif (args.Length < 1) { return 1; }\nvar SpecPath = args[0];\nif (!File.Exists(SpecPath)) { return 2; }\n\nvar Body = await File.ReadAllTextAsync(SpecPath);\nvar Strs = DeleteFilesPatterns.ConstString().Matches(Body)\n    .ToDictionary(M => M.Groups[\"name\"].Value, M => M.Groups[\"value\"].Value, StringComparer.Ordinal);\n\nfor (var I = 1; I <= 99; I++)";
        public const string Replace_01 = "#:include SharedScripts.cs\nusing Scripts;\n\nif (args.Length < 1) { return 1; }\nvar SpecPath = args[0];\nif (!File.Exists(SpecPath)) { return 2; }\n\nvar Strs = await ScratchConfig.LoadStringsAsync(SpecPath);\n\nfor (var I = 1; I <= 99; I++)";

        public const string Find_02 = "namespace Scripts\n{\n    internal static partial class DeleteFilesPatterns\n    {\n        [GeneratedRegex(\"\"\"const\\s+string\\s+(?<name>\\w+)\\s*=\\s*@?\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"\\s*;\"\"\", RegexOptions.ExplicitCapture)]\n        internal static partial Regex ConstString();\n    }\n}";
        public const string Replace_02 = "";
    }
}
