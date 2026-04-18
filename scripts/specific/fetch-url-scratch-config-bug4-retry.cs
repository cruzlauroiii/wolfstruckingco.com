public static class FetchUrlConfig
{
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes =
    [
        ("grep-source", "file:///C:/repo/public/wolfstruckingco.com/main/scripts/generic/grep-content.cs", "save", "main/scripts/specific/grep-content-source.txt", "GET", 0)
    ];
}
