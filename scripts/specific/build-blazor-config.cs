namespace Scripts;

internal static class BuildBlazorConfig
{
    public const string PublishTempDir = "wolfs-blazor-publish";
    public const string BaseHrefSearch = "<base href=\"/\" />";
    public const string BaseHrefReplace = "<base href=\"/wolfstruckingco.com/app/\" />";
    public const string LocalUrl = "http://localhost:8080/wolfstruckingco.com/app/";
    public const string ClientCsprojRel = "src/Client/Client.csproj";
    public const string TargetWwwrootSubdir = "app";
}
