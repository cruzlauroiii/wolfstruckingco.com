return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV155
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = ".TrackHeroDelivered { padding: 28px 18px; text-align: center;\n  background: linear-gradient(180deg, #ecfdf5 0%, #d1fae5 100%);\n  border-color: #10b981;\n  .TrackDeliveredIcon { font-size: 3rem; line-height: 1; margin-bottom: 8px; }\n  .TrackDeliveredTitle { font-size: 1.4rem; font-weight: 800; color: #047857; margin-bottom: 6px; }\n  .TrackDeliveredSub { color: var(--text-muted); font-size: .92rem; }\n}\n";
        public const string Replace_01 = ".TrackHero.TrackHeroDelivered { padding: 28px 18px; text-align: center;\n  background: linear-gradient(180deg, #ecfdf5 0%, #d1fae5 100%);\n  border-color: #10b981;\n  .TrackDeliveredIcon { font-size: 3rem; line-height: 1; margin-bottom: 8px; }\n  .TrackDeliveredTitle { font-size: 1.4rem; font-weight: 800; color: #047857; margin-bottom: 6px; }\n  .TrackDeliveredSub { color: var(--text-muted); font-size: .92rem; }\n}\n";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
