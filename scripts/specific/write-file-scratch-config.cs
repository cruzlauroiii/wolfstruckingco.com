return 0;

namespace Scripts
{
    internal static class WriteFileScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\set-config-scratch-config.cs";
        public const string Content = "return 0;\n\nnamespace Scripts\n{\n    internal static class SetConfigScratchConfig\n    {\n        public const string TargetConfig = @\"C:\\repo\\public\\wolfstruckingco.com\\main\\scripts\\specific\\cat-file-scratch-config.cs\";\n        public const string Set_SourcePath = @\"C:\\Users\\user1\\AppData\\Local\\Temp\\cdp-list-probe.log\";\n    }\n}\n";
        public const string Mode = "overwrite";
    }
}
