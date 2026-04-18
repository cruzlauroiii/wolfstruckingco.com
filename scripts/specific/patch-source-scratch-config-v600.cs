return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV600
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";
        public const string Find_01 = ".HiddenInput { display: none; }";
        public const string Replace_01 = ".HiddenInput { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }";
    }
}
