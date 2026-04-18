return 0;

namespace Scripts
{
    internal static class DumpFileScratchConfigV70
    {
        public const string Path = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Mode = "grep";
        public const string Pattern = "(?i)(scene\\s*17|case\\s*17|sceneIndex\\s*==\\s*17|N\\s*==\\s*17|/Apply|ApplicationStatus|submitted|pending|approval|Route\\s*=|preState|prerender|PreState)";
        public const int LineStart = 1;
        public const int LineEnd = 5;
        public const int TailN = 30;
        public const int BytePos = 0;
        public const int ByteLen = 4096;
    }
}
