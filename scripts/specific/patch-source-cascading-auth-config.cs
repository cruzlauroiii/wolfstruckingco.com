return 0;

namespace Scripts
{
    internal static class PatchSourceCascadingAuthConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\generate-statics.cs";

        public const string Find_01 = "                var Params = ParameterView.FromDictionary(new Dictionary<string, object?>\n                {\n                    [\"Layout\"] = typeof(MainLayout),\n                    [\"ChildContent\"] = (RenderFragment)(B => { B.OpenComponent(0, PageType); B.CloseComponent(); }),\n                });\n                var Output = await Renderer.RenderComponentAsync<LayoutView>(Params);";
        public const string Replace_01 = "                var Params = ParameterView.FromDictionary(new Dictionary<string, object?>\n                {\n                    [\"ChildContent\"] = (RenderFragment)(B => { B.OpenComponent<LayoutView>(0); B.AddComponentParameter(1, \"Layout\", typeof(MainLayout)); B.AddComponentParameter(2, \"ChildContent\", (RenderFragment)(C => { C.OpenComponent(0, PageType); C.CloseComponent(); })); B.CloseComponent(); }),\n                });\n                var Output = await Renderer.RenderComponentAsync<Microsoft.AspNetCore.Components.Authorization.CascadingAuthenticationState>(Params);";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
