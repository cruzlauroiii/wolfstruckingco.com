namespace Scripts;

internal static class WipeDbConfig
{
    public const string WipeUrl = "https://wolfstruckingco.nbth.workers.dev/api-wipe";
    public const string VerifyUrl = "https://wolfstruckingco.nbth.workers.dev/api/listings";
    public const string SessionHeader = "X-Wolfs-Session";
    public const string RoleHeader = "X-Wolfs-Role";
    public const string RoleValue = "admin";
    public const string SessionPrefix = "pipeline_wipe_";
}
