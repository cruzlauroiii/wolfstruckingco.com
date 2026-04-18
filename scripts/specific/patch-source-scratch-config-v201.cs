return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV201
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = "  padding: 12px 16px; border-bottom: 1px solid var(--border);\n  background: #fff; position: sticky; top: 0; z-index: 10; gap: 8px; min-height: 56px;";
        public const string Replace_01 = "  padding: 12px 16px;\n  background: #fff; position: sticky; top: 0; z-index: 10; gap: 8px; min-height: 56px;";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
