return 0;

namespace Scripts
{
    internal static class WriteFileScratchConfigV7
    {
        public const string TargetFile = "main/scripts/specific/SharedSpecifics.cs";
        public const string Mode = "overwrite";
        public const string Content = "return 0;\n\nnamespace Scripts\n{\n    internal static class SharedSpecifics\n    {\n        public const string LiveBaseUrl = \"https://cruzlauroiii.github.io\";\n        public const string LocalBaseUrl = \"https://localhost:8443\";\n        public const string RepoRoot = @\"C:\\repo\\public\\wolfstruckingco.com\";\n        public const string MainRoot = @\"C:\\repo\\public\\wolfstruckingco.com\\main\";\n        public const string GenericDir = @\"C:\\repo\\public\\wolfstruckingco.com\\main\\scripts\\generic\";\n        public const string SpecificDir = @\"C:\\repo\\public\\wolfstruckingco.com\\main\\scripts\\specific\";\n    }\n}\n";
    }
}
