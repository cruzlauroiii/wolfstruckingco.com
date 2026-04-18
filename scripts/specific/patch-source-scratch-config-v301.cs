return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV301
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";
        public const string Find_01 = "    <button class=\"Btn Ghost ChatBtnRound\" type=\"button\" title=\"@AttachTitle\" @onclick=\"AttachAsync\">📎</button>";
        public const string Replace_01 = "    <label class=\"Btn Ghost ChatBtnRound\" title=\"@AttachTitle\" for=\"ChatAttachInput\">📎</label>\n    <InputFile id=\"ChatAttachInput\" OnChange=\"OnFilesAttachedAsync\" class=\"HiddenInput\" />";
    }
}
