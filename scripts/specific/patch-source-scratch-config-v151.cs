return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV151
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\MarketplacePage.razor.cs";

        public const string Find_01 = "        public string SellerEmail { get; set; } = Empty;\r\n\r\n        public string Seller";
        public const string Replace_01 = "        public string SellerEmail { get; set; } = Empty;\r\n\r\n        public string Status { get; set; } = Empty;\r\n\r\n        public string Seller";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
