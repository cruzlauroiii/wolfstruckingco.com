return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV123
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\TrackPage.razor";

        public const string Find_01 = "    <p>@(Latest?[\"subject\"]?.ToString() ?? \"Live shipment status\")</p>\n\n";
        public const string Replace_01 = "";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
</content>
</invoke>