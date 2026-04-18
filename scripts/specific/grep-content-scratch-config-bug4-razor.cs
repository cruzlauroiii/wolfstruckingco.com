return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfig
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components";
        public const string FilePattern = "MainLayout.razor";
        public const string Pattern = @"class=""NavSecondary""|class=""NavTertiary""";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\bug4-razor.txt";
    }
}
