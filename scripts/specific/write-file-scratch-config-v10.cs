return 0;

namespace Scripts
{
    internal static class WriteFileScratchConfigV10
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\ThemeChip.razor";
        public const string Content = "@namespace SharedUI.Components\n\n<button type=\"button\" class=\"wt-theme-chip\" title=\"Switch theme\" @onclick=\"OnCycleAsync\">@Label</button>\n";
        public const string Mode = "overwrite";
    }
}
