return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfig
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src";
        public const string Pattern = @"(SupplyParameterFromQuery|NavigationManager|GetUriWithQueryParameter|Uri\.Query)";
        public const string FilePattern = "*.cs";
        public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-content-output-vBug61b.txt";
    }
}
