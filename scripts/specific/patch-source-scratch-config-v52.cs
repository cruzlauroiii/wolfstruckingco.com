return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV52
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = "  .DocBadge { width: 36px; height: 36px; border-radius: 8px;\n    display: flex; align-items: center; justify-content: center;\n    font-size: 1rem; font-weight: 800; flex: none;\n    background: rgba(148, 163, 184, .18); color: #475569;\n    &.Done { background: rgba(34, 197, 94, .14); color: $success; }\n  }";
        public const string Replace_01 = "  .DocBadge { width: 36px; height: 36px; border-radius: 8px;\n    display: flex; align-items: center; justify-content: center;\n    font-size: 1.05rem; font-weight: 800; flex: none;\n    background: rgba(148, 163, 184, .18); color: #94a3b8;\n    &.Done { background: #16a34a; color: #ffffff;\n      font-size: 1.35rem; font-weight: 900; line-height: 1;\n      box-shadow: 0 0 0 2px rgba(22, 163, 74, .35); }\n  }";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
