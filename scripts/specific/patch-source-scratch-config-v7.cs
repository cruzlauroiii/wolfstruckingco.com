return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV7
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    if (Rt == \"/Sell/Chat/\" && SellTurnIdx < SellerTurns.Length)";
        public const string Replace_01 = "    var IsApplicantChat = Narr.Contains(\"Driver from\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"team driver\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"driver chats\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"sends both scans\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"asks for his\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"asks for the team\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"shares his details\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"name and years driving\", StringComparison.OrdinalIgnoreCase);\n    var IsDispatcherChat = (Narr.Contains(\"dispatcher\", StringComparison.OrdinalIgnoreCase) || Narr.Contains(\"radio\", StringComparison.OrdinalIgnoreCase)) && !IsApplicantChat;\n    var IsSellerChat = Rt == \"/Chat/\" && !IsApplicantChat && !IsDispatcherChat;\n    if ((Rt == \"/Sell/Chat/\" || IsSellerChat) && SellTurnIdx < SellerTurns.Length)";
        public const string Find_02 = "    else if (Rt == \"/Applicant/\" && AppTurnIdx < ApplicantTurns.Length)";
        public const string Replace_02 = "    else if ((Rt == \"/Applicant/\" || IsApplicantChat) && AppTurnIdx < ApplicantTurns.Length)";
        public const string Find_03 = "    else if (Rt == \"/Dispatcher/\" && DispatchTurnIdx < DispatcherTurns.Length && Narr.IndexOf(\"agent \", StringComparison.OrdinalIgnoreCase) < 0)";
        public const string Replace_03 = "    else if ((Rt == \"/Dispatcher/\" || IsDispatcherChat) && DispatchTurnIdx < DispatcherTurns.Length && Narr.IndexOf(\"agent \", StringComparison.OrdinalIgnoreCase) < 0)";
    }
}
