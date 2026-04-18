return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV19
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\SettingsPage.razor";
        public const string Find_01 = "    <button class=\"Btn Ghost\" @onclick=\"SignOutAsync\">Sign out</button>";
        public const string Replace_01 = "    <a class=\"Btn Ghost\" href=\"?signout=1\">Sign out</a>";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
