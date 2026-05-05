using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Components;

public partial class ThemeChip : ComponentBase
{
    private const string ThemeDark = "dark";
    private const string ThemeLight = "light";
    private const string ThemeQueryPrefix = "theme=";
    private const string LabelAuto = "\uD83C\uDF17 Auto";
    private const string LabelDark = "\uD83C\uDF19 Dark";
    private const string LabelLight = "\u2600 Light";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private string Label { get; set; } = LabelAuto;

    protected override async Task OnInitializedAsync()
    {
        var Query = new Uri(Nav.Uri).Query;
        if (Query.Length > 1)
        {
            foreach (var Pair in Query.TrimStart('?').Split('&'))
            {
                if (!Pair.StartsWith(ThemeQueryPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var Forced = Uri.UnescapeDataString(Pair[ThemeQueryPrefix.Length..]).ToLowerInvariant();
                if (string.Equals(Forced, ThemeLight, StringComparison.Ordinal) || string.Equals(Forced, ThemeDark, StringComparison.Ordinal))
                {
                    await Wolfs.ThemeWriteAsync(Forced);
                    Label = LabelFor(Forced);
                    return;
                }
            }
        }

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
