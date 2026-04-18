return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV7
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src\Domain\Constants";
        public const string FilePattern = "*.cs";
        public const string Pattern = @"agent|Agent|RoleAgent|RoleAssistant";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\role-consts.txt";
    }
}
