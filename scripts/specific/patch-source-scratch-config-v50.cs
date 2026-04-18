return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV50
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = ".HomeCtaCard {\n  display: flex; gap: 14px; align-items: center; padding: 16px; margin-top: 14px;\n  border: 2px solid var(--accent);\n  .HomeCtaIcon { font-size: 2rem; flex: none; }\n  .HomeCtaBody { flex: 1; min-width: 0;\n    .HomeCtaTitle { font-weight: 800; font-size: 1rem; margin-bottom: 2px; }\n    .HomeCtaSub { color: var(--text-muted); font-size: .86rem; }\n  }\n  .Btn { flex: none; }\n  &.Drive { margin-top: 10px; border: 1px solid var(--border); }\n  &.Track { margin-top: 10px; border: 1px solid var(--info); }\n}";
        public const string Replace_01 = ".HomeCtaCard {\n  display: flex; gap: 14px; align-items: center; padding: 16px; margin-top: 14px;\n  border: 2px solid var(--accent);\n  .HomeCtaIcon { font-size: 2rem; flex: none; }\n  .HomeCtaBody { flex: 1; min-width: 0;\n    .HomeCtaTitle { font-weight: 800; font-size: 1rem; margin-bottom: 2px; }\n    .HomeCtaSub { color: var(--text-muted); font-size: .86rem; line-height: 1.55; }\n  }\n  .Btn { flex: none; min-height: 48px; }\n  &.Drive { margin-top: 10px; border: 2px solid var(--accent); }\n  &.Track { margin-top: 10px; border: 2px solid var(--accent); }\n}";

        public const string Find_02 = "@media (max-width: 768px) {\n  .Stage { padding: 10vh 0; max-width: 100%;\n    h1, > p.Sub { padding: 0 16px; }\n  }\n  .Card, .Listing, .Stat, .Hero, .LoginWrap { margin-left: 0; margin-right: 0; border-radius: 0; border-left: none; border-right: none; }\n  .Hero { padding: 28px 16px; }";
        public const string Replace_02 = "@media (max-width: 768px) {\n  .Stage { padding: 16px 0 48px; max-width: 100%;\n    h1, > p.Sub { padding: 0 16px; }\n  }\n  .Card, .Listing, .Stat, .Hero, .LoginWrap { margin-left: 0; margin-right: 0; border-radius: 0; border-left: none; border-right: none; }\n  .Hero { padding: 28px 16px;\n    h1 { max-width: 90%; margin-left: auto; margin-right: auto; font-size: clamp(2rem, 8vw, 2.6rem); font-weight: 900; line-height: 1.15; }\n    p { font-size: clamp(1rem, 4.2vw, 1.125rem); line-height: 1.6; max-width: 92%; }\n  }\n  .HomeCtaCard {\n    flex-direction: column; align-items: stretch; text-align: left; gap: 12px; padding: 18px;\n    .HomeCtaIcon { font-size: 2.4rem; }\n    .HomeCtaBody .HomeCtaTitle { font-size: 1.125rem; }\n    .HomeCtaBody .HomeCtaSub { font-size: 1rem; line-height: 1.55; }\n    .Btn { width: 100%; min-height: 52px; font-size: 1rem; padding: 14px 16px; }\n  }";
    }
}
