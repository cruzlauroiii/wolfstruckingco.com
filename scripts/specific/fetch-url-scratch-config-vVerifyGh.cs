namespace WolfsTruckingCo.Scripts.Specific;

public static class FetchUrlScratchConfigVVerifyGh
{
    public const string BaseUrl = "https://api.github.com";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
        ("LatestCommitMain", "/repos/cruzlauroiii/wolfstruckingco.com/commits/main", "save", "main/scripts/specific/verify-gh-main.json", "GET", 1),
        ("LatestPagesBuild", "/repos/cruzlauroiii/wolfstruckingco.com/pages/builds/latest", "save", "main/scripts/specific/verify-gh-pages-build.json", "GET", 1),
    ];
}
