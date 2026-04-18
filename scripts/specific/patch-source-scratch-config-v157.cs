return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV157
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\TrackPage.razor";

        public const string Find_01 = "        <div class=\"TrackLegend\">🏠 Delivered · 1418 Oak Street, Wilmington NC</div>\n        <div class=\"Card TrackHero TrackHeroDelivered\">\n            <div class=\"TrackDeliveredIcon\">✅</div>\n            <div class=\"TrackDeliveredTitle\">Delivered</div>\n            <div class=\"TrackDeliveredSub\">The order arrived safely at the buyer's address.</div>\n        </div>\n";
        public const string Replace_01 = "        <div class=\"TrackLegend\">✅ Delivered · 1418 Oak Street, Wilmington NC</div>\n        <div class=\"Card TrackHero TrackHeroDelivered\">\n            <iframe title=\"1418 Oak Street, Wilmington NC — delivery destination\" src=\"https://www.openstreetmap.org/export/embed.html?bbox=-77.96%2C34.21%2C-77.93%2C34.24&amp;layer=mapnik&amp;marker=34.2257%2C-77.9447\" style=\"width:100%; height:100%; display:block; border:0; min-width:0;\" frameborder=\"0\" scrolling=\"no\" loading=\"lazy\" referrerpolicy=\"no-referrer-when-downgrade\"></iframe>\n        </div>\n";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
