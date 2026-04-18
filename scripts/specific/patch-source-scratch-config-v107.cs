return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV107
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = "  .Stage { padding: 16px 0 48px; max-width: 100%;\n    h1, > p.Sub { padding: 0 16px; }\n  }";
        public const string Replace_01 = "  .Stage { padding-bottom: 48px; max-width: 100%;\n    h1, > p.Sub { padding: 0 16px; }\n  }";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
