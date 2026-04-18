namespace WolfsTruckingCo.Scripts.Specific;

public static class GrepContentScratchConfigVApplyChat
{
    public const string Root = @"C:\repo\public\wolfstruckingco.com\main\docs\Apply";
    public const string FilePattern = "*.html";
    public const string Pattern = "chat with agent|href=\"Chat|href=\"/Chat|href=\"Applicant";
    public const string OutputFile = @"main\scripts\specific\apply-chat-links-grep.txt";
}
