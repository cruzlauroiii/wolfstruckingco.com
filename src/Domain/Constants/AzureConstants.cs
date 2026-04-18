namespace Domain.Constants;

public static class AzureConstants
{
    public const string AuthorizeUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    public const string TokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    public const string UserInfoUrl = "https://graph.microsoft.com/v1.0/me";
    public const string Scope = "openid email profile User.Read";
    public const string ConfigClientId = "Azure:ClientId";
    public const string ConfigClientSecret = "Azure:ClientSecret";
}
