return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV65
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";

        public const string Find_01 = "var FormFills = new List<List<(string Selector, string Value)>>();\r\nstring StickyDriverEmail = Drivers[0].Email;";
        public const string Replace_01 = "var FormFills = new List<List<(string Selector, string Value)>>();\r\nvar ScenesPnf = new HashSet<string>(StringComparer.Ordinal);\r\nstring StickyDriverEmail = Drivers[0].Email;";

        public const string Find_02 = "Sb2.AppendLine(\"| # | Frame | Route | Narration | Extracted Text (OCR) |\");\r\nSb2.AppendLine(\"|---|---|---|---|---|\");";
        public const string Replace_02 = "Sb2.AppendLine(\"| # | Frame | PNF | Route | Narration | Extracted Text (OCR) |\");\r\nSb2.AppendLine(\"|---|---|---|---|---|---|\");";

        public const string Find_03 = "___UNUSED_SLOT___";
        public const string Replace_03 = "";
    }
}
