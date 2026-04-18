return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV129
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\TrackPage.razor";

        public const string Find_01 = "    <div class=\"Card TrackHero\">\n        <svg viewBox=\"0 0 414 220\" width=\"100%\" height=\"220\" preserveAspectRatio=\"xMidYMid slice\">";
        public const string Replace_01 = "    <div class=\"TrackLegend\">🚢 Pacific Ocean · GPS in car</div>\n    <div class=\"Card TrackHero\">\n        <svg viewBox=\"0 0 414 220\" width=\"100%\" height=\"220\" preserveAspectRatio=\"xMidYMid slice\">";

        public const string Find_02 = "            <text x=\"207\" y=\"200\" text-anchor=\"middle\" font-size=\"9\" fill=\"#1e3a8a\" font-weight=\"700\">Pacific Ocean · GPS in car</text>\n        </svg>";
        public const string Replace_02 = "        </svg>";

        public const string Find_03 = "___UNUSED_SLOT___";
        public const string Replace_03 = "";
    }
}
