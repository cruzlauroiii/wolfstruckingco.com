return 0;

namespace Scripts
{
    internal static class WriteFileScratchConfigV8
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ThemeChip.razor.cs";
        public const string Content = "using System.Threading.Tasks;\nusing Microsoft.AspNetCore.Components;\nusing SharedUI.Services;\n\nnamespace SharedUI.Components;\n\npublic partial class ThemeChip : ComponentBase\n{\n    private const string ThemeAuto = \"auto\";\n    private const string ThemeDark = \"dark\";\n    private const string ThemeLight = \"light\";\n    private const string LabelAuto = \"\\uD83C\\uDF17 Auto\";\n    private const string LabelDark = \"\\uD83C\\uDF19 Dark\";\n    private const string LabelLight = \"\\u2600 Light\";\n\n    [Inject]\n    private WolfsInteropService Wolfs { get; set; } = null!;\n\n    private string Label { get; set; } = LabelAuto;\n\n    protected override async Task OnAfterRenderAsync(bool FirstRender)\n    {\n        if (!FirstRender) { return; }\n        var Current = await Wolfs.ThemeReadAsync();\n        Label = LabelFor(Current);\n        StateHasChanged();\n    }\n\n    private async Task OnCycleAsync()\n    {\n        var Next = await Wolfs.ThemeCycleAsync();\n        Label = LabelFor(Next);\n        StateHasChanged();\n    }\n\n    private static string LabelFor(string Theme) => Theme switch\n    {\n        ThemeDark => LabelDark,\n        ThemeLight => LabelLight,\n        _ => LabelAuto,\n    };\n}\n";
        public const string Mode = "overwrite";
    }
}
