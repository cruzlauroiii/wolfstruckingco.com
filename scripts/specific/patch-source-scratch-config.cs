return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\worker\worker.js";
        public const string Find_01 = "After saving a listing via the db_put tool to the listings collection, ALWAYS include the marketplace URL";
        public const string Replace_01 = "After publishing a listing, ALWAYS include the marketplace URL";
        public const string Find_02 = "Avoid database names, internal collection names, API terms, model names, and acronyms unless the user used them first or asks for technical detail.";
        public const string Replace_02 = "Avoid database names, internal collection names, API terms, model names, and acronyms unless the user used them first or asks for technical detail. Never reveal that you used a tool or internal storage. Specifically forbidden in your reply text: R2, db_put, db_get, db_get_blob, collection, applicants, listings, documents, audit, blob, tool, API, JSON. Speak as a normal Wolfs dispatcher would speak to a real user.";
    }
}
