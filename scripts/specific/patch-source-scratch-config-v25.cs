return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV25
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\generate-statics.cs";
        public const string Find_01 = "#:project ../src/SharedUI/SharedUI.csproj";
        public const string Replace_01 = "#:project ../../src/SharedUI/SharedUI.csproj";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
