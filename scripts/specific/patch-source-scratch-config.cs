return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\rebuild-walkthrough-v3.cs";
        public const string Find_01 = "if (Pad == \"016\" || Pad == \"023\" || Pad == \"030\" || Pad == \"037\")";
        public const string Replace_01 = "if (Pad == \"016\" || Pad == \"023\" || Pad == \"029\" || Pad == \"035\")";
    }
}
