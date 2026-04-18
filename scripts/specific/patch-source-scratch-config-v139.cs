return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV139
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes.cs";

        public const string Find_01 = "// Auto delay + recompute — GPS telemetry detects congestion, system recomputes\nAdd(\"/Track/\",         \"GPS telemetry detects heavy congestion ahead on I-10. ETA recomputed automatically.\");";
        public const string Replace_01 = "// Auto delay + recompute — GPS telemetry detects congestion, system recomputes\nAdd(\"/Dispatcher/\",    \"GPS telemetry detects heavy congestion ahead on I-10. ETA recomputed automatically.\");";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
