return 0;

namespace Scripts
{
    internal static class BuildWalkthroughLocalScratchConfig
    {
        public const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
        public const string DiscoverGeneric = @"scripts\generic\discover-live-routes.cs";
        public const string DiscoverConfig = @"scripts\specific\discover-live-routes-scratch-config.cs";
        public const string CaptureGeneric = @"scripts\generic\capture-frames-readonly.cs";
        public const string CaptureConfig = @"scripts\specific\capture-frames-readonly-scratch-config.cs";
        public const string NarrateGeneric = @"scripts\generic\extract-narrations-claude.cs";
        public const string NarrateConfig = @"scripts\specific\extract-narrations-claude-scratch-config.cs";
        public const string TtsGeneric = @"scripts\generic\tts-rotate.cs";
        public const string TtsConfig = @"scripts\specific\tts-rotate-scratch-config.cs";
        public const string EncodeGeneric = @"scripts\generic\encode-clips.cs";
        public const string EncodeConfig = @"scripts\specific\encode-clips-scratch-config.cs";
        public const string ConcatGeneric = @"scripts\generic\concat-walkthrough.cs";
        public const string ConcatConfig = @"scripts\specific\concat-walkthrough-scratch-config.cs";
    }
}
