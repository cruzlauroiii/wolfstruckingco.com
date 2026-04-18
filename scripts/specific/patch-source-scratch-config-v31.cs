return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV31
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes-final.json";
        public const string Find_01 = "    \"narration\": \"Car seller signs in with Google to post a car for sale.\",";
        public const string Replace_01 = "    \"narration\": \"Car seller signs in with Google.\",";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
