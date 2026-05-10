using System.Threading.Tasks;
using Domain.Constants;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class LoginPage
{
    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private Task DemoGoogleAsync() => Wolfs.SsoLoginAsync(SsoProviderConstants.Google).AsTask();

    private Task DemoGithubAsync() => Wolfs.SsoLoginAsync(SsoProviderConstants.GitHub).AsTask();

    private Task DemoMicrosoftAsync() => Wolfs.SsoLoginAsync(SsoProviderConstants.Microsoft).AsTask();

    private Task DemoOktaAsync() => Wolfs.SsoLoginAsync(SsoProviderConstants.Okta).AsTask();
}
