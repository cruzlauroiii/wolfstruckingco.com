return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV161
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";
        public const string Find_01 = ".LoginWrap { width: 100%; max-width: 520px; margin: 30px auto; padding: 0 16px; }";
        public const string Replace_01 = ".LoginWrap { width: 100%; max-width: 520px; margin: 30px auto; padding: 0 16px; min-height: auto; overflow: visible; }";
        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
