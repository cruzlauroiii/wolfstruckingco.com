return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV35
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    var Shot = await Cdp.SendAsync(\"Page.captureScreenshot\", new { format = \"png\" });";
        public const string Replace_01 = "    var Shot = await Cdp.SendOnceAsync(\"Page.captureScreenshot\", new { format = \"png\" }, 90);";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
