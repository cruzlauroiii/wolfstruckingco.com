return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\css\app.css";
        public const string Find_01 = ".MapStage{padding:0;position:relative;";
        public const string Replace_01 = ".Stage.MapStageFull{max-width:none;padding:0;margin:0;width:100vw;height:calc(100vh - 60px)}.MapStage{padding:0;position:relative;";
    }
}
