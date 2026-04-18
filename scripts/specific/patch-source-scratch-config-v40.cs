return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV40
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    var IsApplicantChat = Rt == \"/Chat/\" && (Narr.Contains(\"Driver from\"";
        public const string Replace_01 = "    var IsApplicantChat = Rt == \"/Chat/\" && Pre <= 50 && (Narr.Contains(\"Driver from\"";
        public const string Find_02 = "    var IsDispatcherChat = (Narr.Contains(\"dispatcher\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"radio\", StringComparison.OrdinalIgnoreCase)) && !IsApplicantChat;";
        public const string Replace_02 = "    var IsDispatcherChat = Rt == \"/Chat/\" && Pre > 50 && !IsApplicantChat;";
    }
}
