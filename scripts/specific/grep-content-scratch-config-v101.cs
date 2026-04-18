return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV101
    {
        public const string Pattern = "Authed|LoginRoute|BuyApi|PaymentModeCard|BuyAsync";
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src";
        public const string FilePattern = "*.razor";
        public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-cascade-razor-out.txt";
    }
}
