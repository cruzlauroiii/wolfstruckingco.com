return 0;

namespace Scripts
{
    internal static class PatchSourceCdpCommandsFromSurfaceScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\CdpCommands.cs";
        public const string Find_01 = "        var ScreenshotParams = new JsonObject { [CdpKey.Format] = Format };";
        public const string Replace_01 = "        var ScreenshotParams = new JsonObject { [CdpKey.Format] = Format, [\"fromSurface\"] = false, [\"captureBeyondViewport\"] = true };";
    }
}
