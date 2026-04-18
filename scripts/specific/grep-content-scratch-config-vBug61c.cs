return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfig
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components";
        public const string Pattern = @"\r$";
        public const string FilePattern = "ChatBox.razor";
        public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-content-output-vBug61c.txt";
    }
}
