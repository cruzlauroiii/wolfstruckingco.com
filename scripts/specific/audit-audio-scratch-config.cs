return 0;

namespace Scripts
{
    internal static class AuditAudioScratchConfig
    {
        public const string SourceDir = @"C:\repo\public\wolfstruckingco.com\main\docs\videos";
        public const string Pattern = "scene-*.mp4";
        public const string OutPath = @"C:\Users\user1\AppData\Local\Temp\audio-durations.tsv";
    }
}
