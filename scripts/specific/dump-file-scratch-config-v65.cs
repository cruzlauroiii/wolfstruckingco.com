return 0;

namespace Scripts
{
    internal static class DumpFileScratchConfigV65
    {
        public const string Path = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\wwwroot\css\app.css";
        public const string Mode = "grep";
        public const string Pattern = "(html,body|\\.Stage\\{|min-height:100dvh|overscroll|min-height:0|flex:1 1 auto)";
        public const int LineStart = 1;
        public const int LineEnd = 5;
        public const int TailN = 30;
        public const int BytePos = 0;
        public const int ByteLen = 4096;
    }
}
