return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV340
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\MarketplacePage.razor";
        public const string Find_01 = "<button class=\"Btn\" @onclick=\"() => BuyAsync(L.Id)\">Buy now</button>";
        public const string Replace_01 = "<a class=\"Btn\" href=\"/wolfstruckingco.com/Buy/ShipTo/?listing=@L.Id\">Buy now</a>";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
