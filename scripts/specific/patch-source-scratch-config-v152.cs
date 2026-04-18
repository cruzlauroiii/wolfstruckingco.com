return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV152
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\MarketplacePage.razor";

        public const string Find_01 = "<div class=\"Actions\"><button class=\"Btn\" @onclick=\"() => BuyAsync(L.Id)\">Buy now</button></div>";
        public const string Replace_01 = "<div class=\"Actions\">@if (string.Equals(L.Status, \"closed\", StringComparison.Ordinal)) { <button class=\"Btn\" disabled>Sold</button> } else { <button class=\"Btn\" @onclick=\"() => BuyAsync(L.Id)\">Buy now</button> }</div>";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
