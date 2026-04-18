return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV115
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = ".MapStage {\n  padding: 0; position: relative; height: 100vh; overflow: hidden;\n  background: linear-gradient(180deg, #eef2f7 0%, #dbe2ea 100%);\n}";
        public const string Replace_01 = ".MapStage {\n  padding: 0; position: relative; flex: 1; min-height: 0; overflow: hidden;\n  background: linear-gradient(180deg, #eef2f7 0%, #dbe2ea 100%);\n}";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
