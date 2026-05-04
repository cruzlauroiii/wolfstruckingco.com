namespace Domain.Constants;

public static class WorkerConstants
{
    public const string Origin = "https://wolfstruckingco.nbth.workers.dev";
    public const string ChatAttachPath = "/api/chat-attach";
    public const string ChatAttachUrl = Origin + ChatAttachPath;
    public const string AiPath = "/ai";
    public const string AiUrl = Origin + AiPath;
    public const string OAuthGoogleStart = Origin + "/oauth/google/start";
    public const string OAuthGithubStart = Origin + "/oauth/github/start";
    public const string OAuthMicrosoftStart = Origin + "/oauth/microsoft/start";
    public const string OAuthOktaStart = Origin + "/oauth/okta/start";
}
