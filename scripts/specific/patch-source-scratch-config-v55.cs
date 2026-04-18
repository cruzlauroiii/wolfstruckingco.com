return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV55
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = "// ── Home CTA cards / feature grid (item #39 phase 2) ─────────────────\n.HomeCtaCard {";
        public const string Replace_01 = "// ── Home walkthrough video (top of HomePage) ───────────────────────\n.HomeWalkthrough {\n  margin: 18px 0 6px;\n  display: flex; flex-direction: column; align-items: center; gap: 8px;\n  video {\n    width: 100%; max-width: 720px; height: auto; display: block;\n    border-radius: 14px; background: #000;\n    box-shadow: 0 18px 48px rgba(15, 23, 42, .28), 0 4px 12px rgba(15, 23, 42, .18);\n  }\n  .WalkthroughCaption {\n    color: var(--text-muted); font-size: .88rem; text-align: center; margin: 0;\n  }\n}\n\n// ── Home CTA cards / feature grid (item #39 phase 2) ─────────────────\n.HomeCtaCard {";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
