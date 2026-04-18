return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV27
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\MainLayout.razor";
        public const string Find_01 = "<a class=\"LinkBtn\" href=\"?signout=1\">Sign out (@EmailShort)</a>";
        public const string Replace_01 = "<button type=\"button\" class=\"LinkBtn\" @onclick=\"SignOutAsync\">Sign out (@EmailShort)</button>";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
