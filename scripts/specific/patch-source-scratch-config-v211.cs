return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV211
    {
        public const string TargetFile = "main/scripts/generic/count-lines.cs";

        public const string Find_01 = "using System.Text.RegularExpressions;\nusing Scripts;\n\nif (args.Length < 1) { await Console.Error.WriteLineAsync(\"usage: dotnet run scripts/count-lines.cs scripts/<config>.cs\"); return 1; }\nvar SpecPath = args[0];\nif (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($\"specific not found: {SpecPath}\"); return 2; }\n\nvar ConstRe = CountLinesPatterns.ConstString();\nstring? FilePath = null;\nstring? Root = null;\nstring? Pattern = null;\nforeach (var (Name, Value) in ConstRe.Matches(await File.ReadAllTextAsync(SpecPath)).Select(M => (M.Groups[\"name\"].Value, M.Groups[\"value\"].Value)))\n{";
        public const string Replace_01 = "#:property ExperimentalFileBasedProgramEnableIncludeDirective=true\n#:property TargetFramework=net11.0\n#:include SharedScripts.cs\nusing Scripts;\n\nif (args.Length < 1) { await Console.Error.WriteLineAsync(\"usage: dotnet run scripts/count-lines.cs scripts/<config>.cs\"); return 1; }\nvar SpecPath = args[0];\nif (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($\"specific not found: {SpecPath}\"); return 2; }\n\nvar Strs = await ScratchConfig.LoadStringsAsync(SpecPath);\nstring? FilePath = null;\nstring? Root = null;\nstring? Pattern = null;\nforeach (var (Name, Value) in Strs.Select(K => (K.Key, K.Value)))\n{";

        public const string Find_02 = "namespace Scripts\n{\n    internal static partial class CountLinesPatterns\n    {\n        [GeneratedRegex(\"\"\"const\\s+string\\s+(?<name>\\w+)\\s*=\\s*@?\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"\\s*;\"\"\", RegexOptions.ExplicitCapture)]\n        internal static partial Regex ConstString();\n    }\n}";
        public const string Replace_02 = "";
    }
}
