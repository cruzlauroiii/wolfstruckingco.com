return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV71
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = "html, body { min-height: 100%; }\nbody {\n  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;\n  background: var(--bg); color: var(--text); min-height: 100vh;\n  display: flex; flex-direction: column; -webkit-font-smoothing: antialiased;\n  font-size: var(--fs-body);\n}";
        public const string Replace_01 = "html, body { min-height: 100%; overflow-y: auto; overflow-x: hidden; }\nbody {\n  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;\n  background: var(--bg); color: var(--text); min-height: 100dvh;\n  display: flex; flex-direction: column; -webkit-font-smoothing: antialiased;\n  font-size: var(--fs-body); overscroll-behavior-y: auto;\n}";

        public const string Find_02 = ".Stage {\n  margin: 0 auto; padding: 24px 16px 48px; width: 100%; max-width: 1100px; flex: 1;\n  display: flex; flex-direction: column; justify-content: flex-start;";
        public const string Replace_02 = ".Stage {\n  margin: 0 auto; padding: 24px 16px 48px; width: 100%; max-width: 1100px; flex: 1 1 auto; min-height: 0;\n  display: flex; flex-direction: column; justify-content: flex-start;";

        public const string Find_03 = "___UNUSED_SLOT___";
        public const string Replace_03 = "";
    }
}
