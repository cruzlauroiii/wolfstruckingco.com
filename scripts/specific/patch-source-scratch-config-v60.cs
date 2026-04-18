return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV60
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\pipeline-cdp.cs";

        public const string Find_01 = "await SendAsync(\"Emulation.setDeviceMetricsOverride\", new { width = 540, height = 960, deviceScaleFactor = 2, mobile = true, screenOrientation = new { angle = 0, type = \"portraitPrimary\" } });";
        public const string Replace_01 = "await SendAsync(\"Emulation.setDeviceMetricsOverride\", new { width = 393, height = 852, deviceScaleFactor = 3, mobile = true, screenOrientation = new { angle = 0, type = \"portraitPrimary\" } });";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
