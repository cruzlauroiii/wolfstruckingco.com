return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV53
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = "  .Empty { margin: 0 16px; }\n}";
        public const string Replace_01 = "  .Empty { margin: 0 16px; }\n  .Listing {\n    .Photo { aspect-ratio: 16/10; }\n    .Body { padding: 18px 16px; gap: 10px; }\n    h3 { font-size: 1.15rem; line-height: 1.3; }\n    .Price { font-size: 1.7rem; }\n    .Stock { font-size: .9rem; }\n    .Desc { font-size: .95rem; line-height: 1.55; color: var(--text); overflow: visible; }\n    .Actions { margin-top: 10px; }\n    .Actions .Btn { width: 100%; min-height: 52px; font-size: 1rem; padding: 14px 16px; }\n  }\n}";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
