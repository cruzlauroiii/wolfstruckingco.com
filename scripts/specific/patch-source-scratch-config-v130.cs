return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV130
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = ".TrackHero { padding: 0; margin-bottom: 14px; overflow: hidden;\n  svg { display: block; background: linear-gradient(180deg, #cfe2f3 0%, #7eb8d4 100%); }\n}";
        public const string Replace_01 = ".TrackLegend { text-align: center; color: var(--text-muted); font-size: .84rem;\n  font-weight: 600; margin-bottom: 10px;\n}\n.TrackHero { padding: 0; margin-bottom: 14px; overflow: hidden;\n  svg { display: block; background: linear-gradient(180deg, #cfe2f3 0%, #7eb8d4 100%); }\n}";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
