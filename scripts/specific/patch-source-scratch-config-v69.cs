return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV69
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\MainLayout.razor";

        public const string Find_01 = "    protected override async Task OnInitializedAsync()\n    {\n        var Auth = await Wolfs.AuthGetAsync();\n        Role = Auth.Role; Email = Auth.Email; Authed = !string.IsNullOrEmpty(Role);\n    }";
        public const string Replace_01 = "    protected override async Task OnAfterRenderAsync(bool firstRender)\n    {\n        if (!firstRender) { return; }\n        var Auth = await Wolfs.AuthGetAsync();\n        Role = Auth.Role; Email = Auth.Email; Authed = !string.IsNullOrEmpty(Role);\n        StateHasChanged();\n    }";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
