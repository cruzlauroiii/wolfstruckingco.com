return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\rebuild-walkthrough-v3.cs";
        public const string Find_01 = "        if (Pad == \"058\" || Pad == \"059\" || Pad == \"060\" || Pad == \"061\" || Pad == \"062\")";
        public const string Replace_01 = "        if (Pad == \"058\" || Pad == \"059\" || Pad == \"060\" || Pad == \"061\" || Pad == \"062\" || Pad == \"066\" || Pad == \"067\" || Pad == \"068\")";
        public const string Find_02 = "        if (Pad == \"063\" || Pad == \"064\")";
        public const string Replace_02 = "        if (Pad == \"063\" || Pad == \"064\" || Pad == \"065\" || Pad == \"069\")";
    }
}
