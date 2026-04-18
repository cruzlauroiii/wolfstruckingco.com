return 0;

namespace Scripts
{
    internal static class GrepContentScratchConfigV17
    {
        public const string Root = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI";
        public const string FilePattern = "*.razor";
        public const string Pattern = @"LoginPage|sso=|SsoLoginAsync|ssoLogin|SignOut|Sign out";
        public const string OutputFile = @"C:\Users\user1\AppData\Local\Temp\login-refs.txt";
    }
}
