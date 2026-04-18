return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV38
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    var IsApplicantChat = Narr.Contains(\"Driver from\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"team driver\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"driver chats\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"sends both scans\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"asks for his\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"asks for the team\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"shares his details\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"name and years driving\", StringComparison.OrdinalIgnoreCase);";
        public const string Replace_01 = "    var IsApplicantChat = Rt == \"/Chat/\" && (Narr.Contains(\"Driver from\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"team driver\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"driver chats\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"sends both scans\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"asks for his\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"asks for the team\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"shares his details\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"name and years driving\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"sends the cert\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"sends the papers\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"auto-handling cert\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"chats with the Agent\", StringComparison.OrdinalIgnoreCase));";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
