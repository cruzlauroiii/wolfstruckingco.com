return 0;

namespace Scripts
{
    internal static class AlignLlmScratchConfig
    {
        public const string ScenesPath = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes-final-v2.json";
        public const string OcrJsonPath = @"C:\Users\user1\AppData\Local\Temp\wolfs-ocr.json";
        public const string WorkerUrl = "https://wolfstruckingco.nbth.workers.dev";
        public const string OutputPath = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\narration-llm-audit.md";
        public const string SessionId = "narration-audit";
        public const string Threshold = "0.4";
    }
}
