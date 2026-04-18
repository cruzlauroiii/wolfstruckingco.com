namespace WolfsTruckingCo.Scripts.Specific;

public static class FetchUrlScratchConfigVBug6Poll2
{
    public const string BaseUrl = "https://cruzlauroiii.github.io";
    public static readonly (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [
        ("Bug6Dashboard", "/wolfstruckingco.com/Dashboard/", "grep", "location\\.replace\\('/wolfstruckingco\\.com/'\\)", "GET", 1),
        ("Bug6Admin", "/wolfstruckingco.com/Admin/", "grep", "location\\.replace\\('/wolfstruckingco\\.com/'\\)", "GET", 1),
        ("Bug6Client", "/wolfstruckingco.com/Client/", "grep", "location\\.replace\\('/wolfstruckingco\\.com/'\\)", "GET", 1),
        ("Bug6Settings", "/wolfstruckingco.com/Settings/", "grep", "location\\.replace\\('/wolfstruckingco\\.com/'\\)", "GET", 1),
        ("Bug6Hiring", "/wolfstruckingco.com/Hiring/", "grep", "location\\.replace\\('/wolfstruckingco\\.com/'\\)", "GET", 1),
        ("Bug6Investors", "/wolfstruckingco.com/Investors/", "grep", "location\\.replace\\('/wolfstruckingco\\.com/'\\)", "GET", 1),
    ];
}
