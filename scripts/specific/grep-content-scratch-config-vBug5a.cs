return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigVBug5a
    {
        public const string Pattern = @"\.Hero|padding:\s*1rem 18px 2rem|margin-top:\s*0";
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss";
        public const string FilePattern = "app.scss";
        public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-bug5a-scss-out.txt";
    }
}
