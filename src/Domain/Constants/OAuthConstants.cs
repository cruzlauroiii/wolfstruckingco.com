namespace Domain.Constants;

public static class OAuthConstants
{
    public const int PkceByteLength = 32;
    public const int PkceCookieMaxAgeSeconds = 600;
    public const string CodeChallengeMethodS256 = "S256";
    public const string GrantTypeAuthorizationCode = "authorization_code";
    public const string PkceCookiePrefix = "pkce_";
    public const string ResponseTypeCode = "code";
    public const string ScopeOpenIdEmailProfile = "openid email profile";
}
