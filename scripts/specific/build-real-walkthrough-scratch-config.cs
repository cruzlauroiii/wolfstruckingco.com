return 0;

namespace Scripts
{
    internal static class BuildRealWalkthroughScratchConfig
    {
        public const string Repo = @"C:\repo\public\wolfstruckingco.com";
        public const string DiscoverGeneric = "main/scripts/generic/discover-live-routes.cs";
        public const string DiscoverConfig = "main/scripts/specific/discover-live-routes-scratch-config.cs";
        public const string NarrateGeneric = "main/scripts/generic/extract-narrations-claude.cs";
        public const string NarrateConfig = "main/scripts/specific/extract-narrations-claude-scratch-config.cs";
        public const string MergeGeneric = "main/scripts/generic/merge-scenes-real.cs";
        public const string MergeConfig = "main/scripts/specific/merge-scenes-real-scratch-config.cs";
        public const string RealPipelineGeneric = "main/scripts/generic/run-real-pipeline.cs";
        public const string RealPipelineConfig = "main/scripts/specific/run-real-pipeline-scratch-config.cs";
        public const string TtsGeneric = "main/scripts/generic/tts-rotate.cs";
        public const string TtsConfig = "main/scripts/specific/tts-rotate-scratch-config.cs";
        public const string EncodeGeneric = "main/scripts/generic/encode-clips.cs";
        public const string EncodeConfig = "main/scripts/specific/encode-clips-scratch-config.cs";
        public const string ConcatGeneric = "main/scripts/generic/concat-walkthrough.cs";
        public const string ConcatConfig = "main/scripts/specific/concat-walkthrough-scratch-config.cs";
    }
}
