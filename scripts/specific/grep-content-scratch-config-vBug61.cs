return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfig
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src";
        public const string Pattern = @"(InteractiveServer|InteractiveWebAssembly|InteractiveAuto|AddInteractive|MapRazorComponents|UseStaticWebAssets|prerender|Prerender)";
        public const string FilePattern = "*.cs";
        public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-content-output-vBug61.txt";
    }
}
