return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV125
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\TrackPage.razor";

        public const string Find_01 = "    <div class=\"Card TrackHero\">\n        <svg viewBox=\"0 0 414 220\" width=\"100%\" height=\"220\" preserveAspectRatio=\"xMidYMid slice\">\n            <path d=\"M 0 200 Q 100 195 200 198 Q 300 202 414 200 L 414 220 L 0 220 Z\" fill=\"#2563eb\" opacity=\".22\"/>\n            <text x=\"40\" y=\"40\" font-size=\"11\" fill=\"#1e3a8a\" font-weight=\"700\">Shanghai</text>\n            <circle cx=\"40\" cy=\"50\" r=\"6\" fill=\"#22c55e\" stroke=\"#fff\" stroke-width=\"2\"/>\n            <text x=\"350\" y=\"40\" font-size=\"11\" fill=\"#1e3a8a\" font-weight=\"700\" text-anchor=\"end\">Port of LA</text>\n            <circle cx=\"380\" cy=\"50\" r=\"6\" fill=\"#ff6b35\" stroke=\"#fff\" stroke-width=\"2\"/>\n            <path d=\"M 40 50 Q 200 80 380 50\" fill=\"none\" stroke=\"#22c55e\" stroke-width=\"3\" stroke-dasharray=\"@($\"{ShipProgress},100\")\" pathLength=\"100\"/>\n            <path d=\"M 40 50 Q 200 80 380 50\" fill=\"none\" stroke=\"#94a3b8\" stroke-width=\"2\" stroke-dasharray=\"3 4\"/>\n            <g transform=\"translate(@(ShipX),@(ShipY))\">\n                <circle cx=\"0\" cy=\"0\" r=\"14\" fill=\"#0ea5e9\" opacity=\".22\"/>\n                <text x=\"0\" y=\"6\" text-anchor=\"middle\" font-size=\"20\">🚢</text>\n            </g>\n        </svg>\n    </div>\n";
        public const string Replace_01 = "    <div class=\"Card TrackHero\">\n        <div style=\"display:flex; flex-direction:row; gap:1px; background:#0ea5e9;\">\n            <iframe title=\"Shanghai Yangshan port — Pacific origin\" src=\"https://www.openstreetmap.org/export/embed.html?bbox=121.0%2C30.7%2C122.2%2C31.7&amp;layer=mapnik&amp;marker=31.2%2C121.5\" style=\"flex:1; height:220px; display:block; border:0; min-width:0;\" frameborder=\"0\" scrolling=\"no\" loading=\"lazy\" referrerpolicy=\"no-referrer-when-downgrade\"></iframe>\n            <iframe title=\"Port of Los Angeles — Pacific destination\" src=\"https://www.openstreetmap.org/export/embed.html?bbox=-118.7%2C33.4%2C-118.0%2C33.9&amp;layer=mapnik&amp;marker=33.7%2C-118.2\" style=\"flex:1; height:220px; display:block; border:0; min-width:0;\" frameborder=\"0\" scrolling=\"no\" loading=\"lazy\" referrerpolicy=\"no-referrer-when-downgrade\"></iframe>\n        </div>\n    </div>\n";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
