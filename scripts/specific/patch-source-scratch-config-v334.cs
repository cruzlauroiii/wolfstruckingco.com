return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV334
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = ".Hero {\n  padding: 8vh 18px; text-align: center;\n  background: linear-gradient(135deg, var(--card) 0%, #fff 100%);\n  border-radius: var(--radius); margin-bottom: 20px;\n  h1 { font-size: var(--fs-h1); font-weight: 800; margin-bottom: 14px;\n    span { color: var(--accent); }\n  }\n  p { font-size: var(--fs-body); color: var(--text-muted); max-width: 680px; margin: 0 auto 20px; line-height: 1.6; }\n}";
        public const string Replace_01 = ".Hero {\n  padding: 1rem 18px 2rem; text-align: center;\n  background: linear-gradient(135deg, var(--card) 0%, #fff 100%);\n  border-radius: var(--radius); margin-bottom: 20px;\n  h1 { font-size: var(--fs-h1); font-weight: 800; margin-top: 0; margin-bottom: 14px;\n    span { color: var(--accent); }\n  }\n  p { font-size: var(--fs-body); color: var(--text-muted); max-width: 680px; margin: 0 auto 20px; line-height: 1.6; }\n}";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
