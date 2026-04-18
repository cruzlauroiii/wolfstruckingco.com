public static class Config
{
    public const string TargetFile = "main/src/SharedUI/scss/app.scss";
    public const string Find_01 = "@media (max-width: 768px) {\n  .Stage { padding-bottom: 48px; max-width: 100%;";
    public const string Replace_01 = "@media (min-width: 769px) and (max-width: 1100px) {\n  .TopActions .NavSecondary { display: none; }\n}\n@media (min-width: 769px) and (max-width: 900px) {\n  .TopActions .NavTertiary { display: none; }\n}\n\n@media (max-width: 768px) {\n  .Stage { padding-bottom: 48px; max-width: 100%;";
}
