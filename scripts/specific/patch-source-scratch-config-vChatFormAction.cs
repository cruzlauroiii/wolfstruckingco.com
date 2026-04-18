namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVChatFormAction
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor";
    public const string Find_01 = "<form class=\"ChatInputRow\" action=\"/Chat/\" method=\"get\">";
    public const string Replace_01 = "<form class=\"ChatInputRow\" action=\"/wolfstruckingco.com/Chat/\" method=\"get\">";
}
