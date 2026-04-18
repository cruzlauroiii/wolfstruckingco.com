return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV146
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes-final.json";

        public const string Find_01 = "\"target\": \"https://localhost:8443/wolfstruckingco.com/Schedule/?cb=084\",\r\n    \"narration\": \"System recomputes downstream legs from live traffic.\",\r\n    \"wait\": 3\r\n  },\r\n  {\r\n    \"action\": \"navigate\",\r\n    \"target\": \"https://localhost:8443/wolfstruckingco.com/Chat/?cb=085\",\r\n    \"narration\": \"Agent tells the buyer the new ETA.\",";
        public const string Replace_01 = "\"target\": \"https://localhost:8443/wolfstruckingco.com/Dispatcher/?cb=084\",\r\n    \"narration\": \"Agent tells the buyer the new ETA.\",\r\n    \"wait\": 3\r\n  },\r\n  {\r\n    \"action\": \"navigate\",\r\n    \"target\": \"https://localhost:8443/wolfstruckingco.com/Schedule/?cb=085\",\r\n    \"narration\": \"System recomputes downstream legs from live traffic.\",";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
