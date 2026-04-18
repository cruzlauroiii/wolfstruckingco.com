namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVRevertStreamRef
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";
    public const string Find_01 = "<div class=\"ChatStream\" id=\"ChatStream\" @ref=\"StreamRef\">";
    public const string Replace_01 = "<div class=\"ChatStream\" id=\"ChatStream\">";
}
