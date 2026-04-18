namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVCallVoice
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";
    public const string Find_01 = "<a class=\"Btn Ghost ChatBtnRound\" href=\"tel:+15555550100\" title=\"@CallTitle\" role=\"button\">📞</a>";
    public const string Replace_01 = "<a class=\"Btn Ghost ChatBtnRound\" href=\"Voice\" title=\"@CallTitle\" role=\"button\">📞</a>";
}
