return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV28
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\SettingsPage.razor.cs";
        public const string Find_01 = "        Role = Auth.Role ?? Dash;\n    }\n}";
        public const string Replace_01 = "        Role = Auth.Role ?? Dash;\n    }\n\n    private async Task SignOutAsync()\n    {\n        await Wolfs.AuthClearAsync();\n        Email = Dash;\n        Role = Dash;\n        StateHasChanged();\n    }\n}";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
