return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV3
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Services\WolfsInteropService.cs";
        public const string Find_01 = "using System.Text.Json;\nusing Microsoft.JSInterop;\n\nnamespace SharedUI.Services;\n\npublic sealed class WolfsInteropService(IJSRuntime Js)";
        public const string Replace_01 = "using System.Text.Json;\nusing Microsoft.AspNetCore.Components.Authorization;\nusing Microsoft.JSInterop;\n\nnamespace SharedUI.Services;\n\npublic sealed class WolfsInteropService(IJSRuntime Js, AuthenticationStateProvider AuthState)";
        public const string Find_02 = "    public ValueTask<AuthState> AuthGetAsync() => Js.InvokeAsync<AuthState>(AuthGet);\n    public ValueTask AuthSetAsync(string? Role, string? Email, string? Session)\n        => Js.InvokeVoidAsync(AuthSet, Role, Email, Session);\n    public ValueTask AuthClearAsync() => Js.InvokeVoidAsync(AuthClear);";
        public const string Replace_02 = "    public ValueTask<AuthState> AuthGetAsync() => Js.InvokeAsync<AuthState>(AuthGet);\n    public async ValueTask AuthSetAsync(string? Role, string? Email, string? Session)\n    {\n        await Js.InvokeVoidAsync(AuthSet, Role, Email, Session);\n        if (AuthState is LocalStorageAuthStateProvider Provider) { Provider.NotifyChanged(); }\n    }\n    public async ValueTask AuthClearAsync()\n    {\n        await Js.InvokeVoidAsync(AuthClear);\n        if (AuthState is LocalStorageAuthStateProvider Provider) { Provider.NotifyChanged(); }\n    }";
    }
}
