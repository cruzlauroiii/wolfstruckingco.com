return 0;

namespace Scripts
{
    internal static class RunRealPipelineScratchConfig
    {
        public const string ScenesPath = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes-real.json";
        public const string FrameDir = @"C:\Users\user1\AppData\Local\Temp\wolfs-walkthrough\real-frames";
        public const string Repo = @"C:\repo\public\wolfstruckingco.com";
        public const string ChromeExe = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        public const string RealRenderGeneric = "main/scripts/generic/real-render.cs";
        public const string RealRenderConfig = "main/scripts/specific/real-render-scratch-config.cs";
        public const string RequestHumanGeneric = "main/scripts/generic/request-human.cs";
        public const string RequestHumanConfig = "main/scripts/specific/request-human-scratch-config.cs";
        public const int DebugPort = 9222;
        public const int HydrateMs = 6000;
    }
}
