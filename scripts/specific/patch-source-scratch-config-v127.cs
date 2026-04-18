return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV127
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = ".TrackStage { padding: 14px;";
        public const string Replace_01 = ".TrackStage { padding: 14px; flex: 1; min-height: 0;";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
