using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class SettingsPage
{
    private const string Dash = "—";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private string Email { get; set; } = Dash;

    private string Role { get; set; } = Dash;

    protected override async Task OnInitializedAsync()
    {
        var Auth = await Wolfs.AuthGetAsync();
        Email = Auth.Email ?? Dash;
        Role = Auth.Role ?? Dash;
    }

    private async Task SignOutAsync()
    {
        await Wolfs.AuthClearAsync();
        Email = Dash;
        Role = Dash;
        StateHasChanged();
    }
}
