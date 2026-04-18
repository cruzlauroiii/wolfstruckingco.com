namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVAttachName
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";
    public const string Find_01 = "<label class=\"Btn Ghost ChatBtnRound\" title=\"@AttachTitle\"><input type=\"file\" hidden multiple />📎</label>";
    public const string Replace_01 = "<label class=\"Btn Ghost ChatBtnRound\" title=\"@AttachTitle\"><input type=\"file\" name=\"files\" accept=\"image/*,application/pdf\" hidden multiple />📎</label>";
}
