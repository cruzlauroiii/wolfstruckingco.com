return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV111
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = ".PageStage { padding: 14px;\n  > h1 { font-size: 1.4rem; margin-bottom: 6px; }\n  > p { color: var(--text-muted); font-size: .9rem; margin-bottom: 14px; }\n  > h2 { font-size: 1rem; margin-bottom: 10px; }\n}";
        public const string Replace_01 = ".PageStage { padding: 0 14px 14px;\n  > h1 { font-size: 1.4rem; margin-bottom: 6px; }\n  > p { color: var(--text-muted); font-size: .9rem; margin-bottom: 14px; }\n  > h2 { font-size: 1rem; margin-bottom: 10px; }\n}";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
