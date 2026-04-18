return 0;

namespace Scripts
{
    internal static class PatchSourceSetconfigFixConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\set-config.cs";
        public const string Find_01 = "    internal static Regex FieldAnchor(string FieldName) => new($\"(\\\\s+public\\\\s+const\\\\s+string\\\\s+{Regex.Escape(FieldName)}\\\\s*=\\\\s*)(@?)\\\"(?:[^\\\"\\\\\\\\]|\\\\\\\\.)*\\\"\\\\s*;\", RegexOptions.ExplicitCapture);";
        public const string Replace_01 = "    internal static Regex FieldAnchor(string FieldName) => new($\"(?<prefix>\\\\s+public\\\\s+const\\\\s+string\\\\s+{Regex.Escape(FieldName)}\\\\s*=\\\\s*)(?<verb>@?)\\\"(?:[^\\\"\\\\\\\\]|\\\\\\\\.)*\\\"\\\\s*;\");";
        public const string Find_02 = "    var NewLine = $\"{Match.Groups[1].Value}{Match.Groups[2].Value}\\\"{NewValue.Replace(\"\\\"\", \"\\\\\\\"\", StringComparison.Ordinal)}\\\";\";";
        public const string Replace_02 = "    var NewLine = $\"{Match.Groups[\\\"prefix\\\"].Value}{Match.Groups[\\\"verb\\\"].Value}\\\"{NewValue.Replace(\"\\\"\", \"\\\\\\\"\", StringComparison.Ordinal)}\\\";\";";
    }
}
