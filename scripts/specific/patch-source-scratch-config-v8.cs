return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV8
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";
        public const string Find_01 = ".ChatBubble {\n  max-width: 80%; padding: 10px 14px; font-size: .9rem;";
        public const string Replace_01 = ".ChatBubble {\n  max-width: 88%; padding: 12px 16px; font-size: .95rem; line-height: 1.45;";
        public const string Find_02 = ".ChatInputRow {\n  display: flex; gap: 6px; align-items: center; background: #fff;";
        public const string Replace_02 = ".ChatInputRow {\n  display: flex; gap: 6px; align-items: center; justify-content: flex-end; background: #fff;";
    }
}
