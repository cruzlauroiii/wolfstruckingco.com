return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV41
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    else if ((Rt == \"/Dispatcher/\" || IsDispatcherChat) && DispatchTurnIdx < DispatcherTurns.Length && Narr.IndexOf(\"agent \", StringComparison.OrdinalIgnoreCase) < 0)";
        public const string Replace_01 = "    else if ((Rt == \"/Dispatcher/\" || IsDispatcherChat) && DispatchTurnIdx < DispatcherTurns.Length)";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
