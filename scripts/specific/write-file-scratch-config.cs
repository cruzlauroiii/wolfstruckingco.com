return 0;

namespace Scripts
{
    internal static class WriteFileScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\patch-source-scratch-config.cs";
        public const string Content = "return 0;\n\nnamespace Scripts\n{\n    internal static class PatchSourceScratchConfig\n    {\n        public const string TargetFile = @\"C:\\repo\\public\\wolfstruckingco.com\\main\\scripts\\generic\\rebuild-walkthrough-v3.cs\";\n        public const string Find_01 = \"if (Pad == \\\"016\\\" || Pad == \\\"023\\\" || Pad == \\\"030\\\" || Pad == \\\"037\\\")\";\n        public const string Replace_01 = \"if (Pad == \\\"016\\\" || Pad == \\\"023\\\" || Pad == \\\"029\\\" || Pad == \\\"035\\\")\";\n    }\n}\n";
        public const string Mode = "overwrite";
    }
}
