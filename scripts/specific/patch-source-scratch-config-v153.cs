return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV153
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\TrackPage.razor";

        public const string Find_01 = "    <div class=\"TrackLegend\">🚢 Pacific Ocean · GPS in car</div>\n    <div class=\"Card TrackHero\">\n        <div style=\"display:flex; flex-direction:row; gap:1px; background:#0ea5e9;\">\n            <iframe title=\"Shanghai Yangshan port — Pacific origin\" src=\"https://www.openstreetmap.org/export/embed.html?bbox=121.0%2C30.7%2C122.2%2C31.7&amp;layer=mapnik&amp;marker=31.2%2C121.5\" style=\"flex:1; height:220px; display:block; border:0; min-width:0;\" frameborder=\"0\" scrolling=\"no\" loading=\"lazy\" referrerpolicy=\"no-referrer-when-downgrade\"></iframe>\n            <iframe title=\"Port of Los Angeles — Pacific destination\" src=\"https://www.openstreetmap.org/export/embed.html?bbox=-118.7%2C33.4%2C-118.0%2C33.9&amp;layer=mapnik&amp;marker=33.7%2C-118.2\" style=\"flex:1; height:220px; display:block; border:0; min-width:0;\" frameborder=\"0\" scrolling=\"no\" loading=\"lazy\" referrerpolicy=\"no-referrer-when-downgrade\"></iframe>\n        </div>\n    </div>\n";
        public const string Replace_01 = "    @if (string.Equals(Latest?[\"subject\"]?.ToString(), \"Delivered\", StringComparison.Ordinal))\n    {\n        <div class=\"TrackLegend\">🏠 Delivered · 1418 Oak Street, Wilmington NC</div>\n        <div class=\"Card TrackHero TrackHeroDelivered\">\n            <div class=\"TrackDeliveredIcon\">✅</div>\n            <div class=\"TrackDeliveredTitle\">Delivered</div>\n            <div class=\"TrackDeliveredSub\">The order arrived safely at the buyer's address.</div>\n        </div>\n    }\n    else\n    {\n        <div class=\"TrackLegend\">🚢 Pacific Ocean · GPS in car</div>\n        <div class=\"Card TrackHero\">\n            <div style=\"display:flex; flex-direction:row; gap:1px; background:#0ea5e9;\">\n                <iframe title=\"Shanghai Yangshan port — Pacific origin\" src=\"https://www.openstreetmap.org/export/embed.html?bbox=121.0%2C30.7%2C122.2%2C31.7&amp;layer=mapnik&amp;marker=31.2%2C121.5\" style=\"flex:1; height:220px; display:block; border:0; min-width:0;\" frameborder=\"0\" scrolling=\"no\" loading=\"lazy\" referrerpolicy=\"no-referrer-when-downgrade\"></iframe>\n                <iframe title=\"Port of Los Angeles — Pacific destination\" src=\"https://www.openstreetmap.org/export/embed.html?bbox=-118.7%2C33.4%2C-118.0%2C33.9&amp;layer=mapnik&amp;marker=33.7%2C-118.2\" style=\"flex:1; height:220px; display:block; border:0; min-width:0;\" frameborder=\"0\" scrolling=\"no\" loading=\"lazy\" referrerpolicy=\"no-referrer-when-downgrade\"></iframe>\n            </div>\n        </div>\n    }\n";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
