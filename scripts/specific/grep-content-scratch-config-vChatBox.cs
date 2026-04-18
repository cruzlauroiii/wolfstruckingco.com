namespace WolfsTruckingCo.Scripts.Specific;

public static class GrepContentScratchConfigVChatBox
{
    public const string Root = @"main\scripts\specific";
    public const string FilePattern = "chat-live.html";
    public const string Pattern = "ChatInputRow|ChatBtnRound|tel:|action=|name=\"msg\"|InputFile|HiddenInput";
    public const string OutputFile = @"main\scripts\specific\chat-live-grep-output.txt";
}
