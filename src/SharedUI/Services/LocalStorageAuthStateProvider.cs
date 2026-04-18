using System.Security.Claims;
using System.Threading.Tasks;
using Domain.Constants;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace SharedUI.Services;

public sealed class LocalStorageAuthStateProvider(IJSRuntime Js) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var Role = await Js.InvokeAsync<string?>(AuthConstants.LocalStorageGetItem, AuthConstants.RoleStorageKey);
            if (string.IsNullOrEmpty(Role))
            {
                return Anonymous;
            }

            var Email = await Js.InvokeAsync<string?>(AuthConstants.LocalStorageGetItem, AuthConstants.EmailStorageKey) ?? string.Empty;
            var Identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, Role),
                new Claim(ClaimTypes.Name, Email),
            ], AuthConstants.LocalAuthScheme);
            return new AuthenticationState(new ClaimsPrincipal(Identity));
        }
        catch (InvalidOperationException)
        {
            return Anonymous;
        }
    }

    public void NotifyChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
