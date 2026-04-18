public static class GrepContentConfig
{
    public static readonly (string Label, string Path, string Mode, string Pattern)[] Probes =
    [
        ("bug4-retry", "main/scripts/specific/bug4-retry-response.html", "match", "NavSecondary")
    ];
}
