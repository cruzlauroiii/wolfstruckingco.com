namespace Domain.Constants;

public static class OktaConstants
{
    public const string AuthorizePathSuffix = "/oauth2/default/v1/authorize";
    public const string TokenPathSuffix = "/oauth2/default/v1/token";
    public const string UserInfoPathSuffix = "/oauth2/default/v1/userinfo";
    public const string Scope = "openid email profile";
    public const string ConfigClientId = "Okta:ClientId";
    public const string ConfigClientSecret = "Okta:ClientSecret";
    public const string ConfigDomain = "Okta:Domain";
}
