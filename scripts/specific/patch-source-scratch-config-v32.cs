return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV32
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\MarketplacePage.razor";
        public const string Find_01 = "                            <div class=\"PhotoCredit\"><span>📷</span><span>Photo by @(string.IsNullOrEmpty(L.Seller) ? \"seller\" : L.Seller) · uploaded at listing</span></div>\n                        </div>";
        public const string Replace_01 = "                        </div>";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
