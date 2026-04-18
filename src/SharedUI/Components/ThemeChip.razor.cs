using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Components;

public partial class ThemeChip : ComponentBase
{
    private const string ThemeDark = "dark";
    private const string ThemeLight = "light";
    private const string LabelAuto = "\uD83C\uDF17 Auto";
    private const string LabelDark = "\uD83C\uDF19 Dark";
    private const string LabelLight = "\u2600 Light";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private string Label { get; set; } = LabelAuto;

    protected override async Task OnInitializedAsync()
    {
        var Current = await Wolfs.ThemeReadAsync();
        Label = LabelFor(Current);
    }

    private static string LabelFor(string Theme) => Theme switch
    {
        ThemeDark => LabelDark,
        ThemeLight => LabelLight,
        _ => LabelAuto,
    };

    private async Task OnCycleAsync()
    {
        var Next = await Wolfs.ThemeCycleAsync();
        Label = LabelFor(Next);
    }
}
