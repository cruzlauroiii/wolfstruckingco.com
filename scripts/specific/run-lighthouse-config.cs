namespace Scripts;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Indexed page list field by design")]
internal static class RunLighthouseConfig
{
    public const string BaseUrl = "https://localhost:8443/wolfstruckingco.com/";
    public const string ReportSubdir = "docs/videos/lighthouse-reports";

    public const string Page_01 = "";
    public const string Page_02 = "About";
    public const string Page_03 = "Services";
    public const string Page_04 = "Pricing";
    public const string Page_05 = "Marketplace";
    public const string Page_06 = "Login";
    public const string Page_07 = "Dashboard";
    public const string Page_08 = "HiringHall";
    public const string Page_09 = "Settings";
}
