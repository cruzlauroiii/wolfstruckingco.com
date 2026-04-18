namespace WolfsTruckingCo.Domain.Constants;

public static class GoogleConstants
{
    public const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string TokenUrl = "https://oauth2.googleapis.com/token";
    public const string UserInfoUrl = "https://www.googleapis.com/oauth2/v3/userinfo";
    public const string Scope = "openid email profile";
    public const string ConfigClientId = "Google:ClientId";
    public const string ConfigClientSecret = "Google:ClientSecret";
}
