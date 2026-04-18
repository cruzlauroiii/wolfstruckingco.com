return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV100
    {
        public const string Pattern = "LoginRoute|BuyApi|PaymentModeCard";
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src";
        public const string FilePattern = "*.cs";
        public const string OutputFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\specific\grep-cascade-out.txt";
    }
}
