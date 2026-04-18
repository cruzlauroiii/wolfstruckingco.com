return 0;

namespace Scripts
{
    internal static class DumpFileScratchConfigV60
    {
        public const string Path = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\pipeline-cdp.cs";
        public const string Mode = "grep";
        public const string Pattern = "(?i)(setDeviceMetricsOverride|deviceScaleFactor|width\\s*=\\s*540|height\\s*=\\s*960|portraitPrimary|screenOrientation|mobile\\s*=\\s*true)";
        public const int LineStart = 1;
        public const int LineEnd = 5;
        public const int TailN = 30;
        public const int BytePos = 0;
        public const int ByteLen = 4096;
    }
}
