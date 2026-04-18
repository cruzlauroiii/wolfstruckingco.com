return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV336
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\MarketplacePage.razor.cs";

        public const string Find_01 = "    private const string ListingsStore = \"listings\";\r\n    private const string LoginRoute = \"Login\";\r\n    private const string BuyApi = \"/api/buy\";\r\n    private const string PaymentModeCard = \"card\";\r\n    private const string RoleEmployer = \"employer\";";
        public const string Replace_01 = "    private const string ListingsStore = \"listings\";\r\n    private const string RoleEmployer = \"employer\";";

        public const string Find_02 = "    [Inject]\r\n    private WolfsInteropService Wolfs { get; set; } = null!;\r\n\r\n    [Inject]\r\n    private NavigationManager Nav { get; set; } = null!;\r\n\r\n    private bool Authed { get; set; }";
        public const string Replace_02 = "    [Inject]\r\n    private WolfsInteropService Wolfs { get; set; } = null!;\r\n\r\n    private bool Authed { get; set; }";

        public const string Find_03 = "___UNUSED_SLOT___";
        public const string Replace_03 = "";
    }
}
