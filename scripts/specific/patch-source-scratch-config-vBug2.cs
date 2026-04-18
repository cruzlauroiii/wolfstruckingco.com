public static class Config
{
    public const string TargetFile = "main/src/SharedUI/Components/ChatBox.razor";
    public const string Find_01 = "    <label class=\"Btn Ghost ChatBtnRound\" title=\"@AttachTitle\" for=\"ChatAttachInput\">📎</label>\n    <InputFile id=\"ChatAttachInput\" OnChange=\"OnFilesAttachedAsync\" class=\"HiddenInput\" />";
    public const string Replace_01 = "    <label class=\"Btn Ghost ChatBtnRound\" title=\"@AttachTitle\"><input type=\"file\" hidden multiple />📎</label>";
}
