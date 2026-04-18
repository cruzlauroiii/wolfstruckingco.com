return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV67
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\scss\app.scss";

        public const string Find_01 = "  .BurgerBtn { background: $dark-card; border-color: $dark-border;\n    span, span::before, span::after { background: $dark-text; }\n  }\n  [data-wolfs-banner=\"1\"] { background: $dark-card; color: $dark-text; border: 1px solid $dark-border; }\n}";
        public const string Replace_01 = "  .BurgerBtn { background: $dark-card; border-color: $dark-border;\n    span, span::before, span::after { background: $dark-text; }\n  }\n  .MenuToggle:checked ~ .BurgerBtn { background: $dark-border; border-color: $dark-accent; }\n  .TopActions { background: $dark-card; border-color: $dark-border; box-shadow: 0 8px 24px rgba(0, 0, 0, .55);\n    a, .LinkBtn { color: $dark-text-muted;\n      &:hover { background: $dark-border; color: $dark-accent; }\n    }\n  }\n  [data-wolfs-banner=\"1\"] { background: $dark-card; color: $dark-text; border: 1px solid $dark-border; }\n}";

        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
