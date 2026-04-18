namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVAttachInput
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";
    public const string Find_01 = "<label class=\"Btn Ghost ChatBtnRound\" title=\"@AttachTitle\"><input type=\"file\" name=\"files\" accept=\"image/*,application/pdf\" hidden multiple />📎</label>";
    public const string Replace_01 = "<label class=\"Btn Ghost ChatBtnRound\" title=\"@AttachTitle\" for=\"ChatAttachInput\">📎</label><InputFile id=\"ChatAttachInput\" OnChange=\"OnFilesAttachedAsync\" class=\"HiddenInput\" multiple accept=\"image/*,application/pdf\" />";
}
