public static class Config
{
    public const string TargetFile = "main/src/SharedUI/Components/MainLayout.razor";
    public const string Find_01 = "<a href=\"About\">About</a>";
    public const string Replace_01 = "<a href=\"About\" class=\"NavSecondary\">About</a>";
    public const string Find_02 = "<a href=\"Services\">Services</a>";
    public const string Replace_02 = "<a href=\"Services\" class=\"NavSecondary\">Services</a>";
    public const string Find_03 = "<a href=\"Pricing\">Pricing</a>";
    public const string Replace_03 = "<a href=\"Pricing\" class=\"NavSecondary\">Pricing</a>";
    public const string Find_04 = "<a href=\"Apply\" class=\"AccentLink\">";
    public const string Replace_04 = "<a href=\"Apply\" class=\"AccentLink NavTertiary\">";
    public const string Find_05 = "<a href=\"@Home\">Dashboard</a>";
    public const string Replace_05 = "<a href=\"@Home\" class=\"NavTertiary\">Dashboard</a>";
}
