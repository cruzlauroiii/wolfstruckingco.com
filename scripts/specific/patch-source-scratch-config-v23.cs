return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV23
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\MainLayout.razor";
        public const string Find_01 = "    private async Task SignOutAsync()\n    {\n        await Wolfs.AuthClearAsync();\n        Authed = false; Email = null; Role = null;\n        Nav.NavigateTo(\"\", forceLoad: false);\n    }\n}";
        public const string Replace_01 = "}";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
