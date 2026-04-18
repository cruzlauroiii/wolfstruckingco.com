return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV169
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\TrackPage.razor";

        public const string Find_01 = "src=\"https://www.openstreetmap.org/export/embed.html?bbox=121.0%2C30.7%2C122.2%2C31.7&amp;layer=mapnik&amp;marker=31.2%2C121.5\"";
        public const string Replace_01 = "src=\"https://www.bing.com/maps/embed/?v=2&amp;cp=31.2~121.5&amp;lvl=10&amp;sty=r&amp;sp=point.31.2_121.5_Shanghai\"";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
