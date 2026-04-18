return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfig
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages";
        public const string Pattern = "BuyAsync|href=\"/wolfstruckingco.com/Buy/ShipTo/";
        public const string FilePattern = "MarketplacePage.razor";
        public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-content-output-vBug7a.txt";
    }
}
