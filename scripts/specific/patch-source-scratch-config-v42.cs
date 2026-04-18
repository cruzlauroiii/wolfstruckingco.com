return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV42
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    (\"At the buyer's door.\", \"buyer\"),\r\n    (\"Job complete. Thanks!\", \"close\"),\r\n];";
        public const string Replace_01 = "    (\"At the buyer's door.\", \"buyer\"),\r\n    (\"Calling the buyer from the front porch now.\", \"call-buyer\"),\r\n    (\"Buyer Sam Carter just opened the door.\", \"buyer-greet\"),\r\n    (\"Buyer is walking around the BYD Han EV, inspecting it.\", \"buyer-inspect\"),\r\n    (\"Buyer paid in full at the door — RTP confirmed.\", \"buyer-pay\"),\r\n    (\"Delivery photo taken and uploaded.\", \"delivery-photo\"),\r\n    (\"Keys handed over. Buyer signed.\", \"keys-handover\"),\r\n    (\"Job complete. Thanks!\", \"close\"),\r\n];";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
