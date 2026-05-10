return 0;

namespace Scripts
{
    internal static class PatchSourceCdpCommandsActivateScreenshotScratchConfigV1
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\CdpCommands.cs";
        public const string Find_01 = "        var Mobile = Args.TryGetValue(\"mobile\", out var MobileArg)";
        public const string Replace_01 = "        var ActivateTargets = await GetPageTargetsAsync(); if (Args.TryGetValue(CdpArg.PageId, out var ScreenPageId)) { var SIdx = int.Parse(ScreenPageId.ToString()!, System.Globalization.CultureInfo.InvariantCulture) - 1; if (SIdx >= 0 && SIdx < ActivateTargets.Count) { await SendBrowserCommandAsync(Cdp.TargetActivateTarget, new JsonObject { [CdpKey.TargetId] = ActivateTargets[SIdx][CdpKey.TargetId]!.ToString() }); await Task.Delay(800); } } var Mobile = Args.TryGetValue(\"mobile\", out var MobileArg)";
    }
}
