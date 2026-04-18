namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVChatScrollCs
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ChatBox.razor.cs";
    public const string Find_01 = "    [Inject]\n    private WolfsInteropService Wolfs { get; set; } = null!;";
    public const string Replace_01 = "    [Inject]\n    private WolfsInteropService Wolfs { get; set; } = null!;\n\n    [Inject]\n    private IJSRuntime Js { get; set; } = null!;\n\n    private ElementReference StreamRef { get; set; }";
    public const string Find_02 = "using Microsoft.AspNetCore.Components.Web;\nusing SharedUI.Services;";
    public const string Replace_02 = "using Microsoft.AspNetCore.Components.Web;\nusing Microsoft.JSInterop;\nusing SharedUI.Services;";
    public const string Find_03 = "    private async Task OnKeyAsync(KeyboardEventArgs E)\n    {\n        if (string.Equals(E.Key, EnterKey, StringComparison.Ordinal) && !E.ShiftKey) { await SendAsync(); }\n    }";
    public const string Replace_03 = "    private async Task OnKeyAsync(KeyboardEventArgs E)\n    {\n        if (string.Equals(E.Key, EnterKey, StringComparison.Ordinal) && !E.ShiftKey) { await SendAsync(); }\n    }\n\n    protected override async Task OnAfterRenderAsync(bool FirstRender)\n    {\n        try\n        {\n            await Js.InvokeVoidAsync(\"window.scrollTo\", 0, int.MaxValue);\n        }\n#pragma warning disable CA1031\n        catch\n#pragma warning restore CA1031\n        {\n        }\n    }";
}
