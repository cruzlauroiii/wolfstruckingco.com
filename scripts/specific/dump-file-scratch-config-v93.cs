return 0;

namespace Scripts
{
    internal static class DumpFileScratchConfigV93
    {
        public const string Path = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes-final.json";
        public const string Mode = "grep";
        public const string Pattern = "\"index\":\\s*7[2-5]\\b|scenes/0?7[2-5]|scene-7[2-5]|/(?:Map|Voyage|Track|Ocean|Ship)/";
        public const int LineStart = 1;
        public const int LineEnd = 1;
        public const int TailN = 30;
        public const int BytePos = 0;
        public const int ByteLen = 4096;
    }
}
