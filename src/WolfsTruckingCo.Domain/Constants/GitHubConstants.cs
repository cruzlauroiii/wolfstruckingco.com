namespace WolfsTruckingCo.Domain.Constants;

public static class GitHubConstants
{
    public const string AuthorizeUrl = "https://github.com/login/oauth/authorize";
    public const string TokenUrl = "https://github.com/login/oauth/access_token";
    public const string UserApiUrl = "https://api.github.com/user";
    public const string Scope = "read:user user:email";
    public const string ConfigClientId = "GitHub:ClientId";
    public const string ConfigClientSecret = "GitHub:ClientSecret";
}
