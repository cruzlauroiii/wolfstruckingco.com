namespace WolfsTruckingCo.Scripts.Specific;

public static class PatchSourceScratchConfigVPublishTrim
{
    public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\publish-pages.cs";
    public const string Find_01 = "        internal static partial Regex BaseHref();\n    }\n}\n";
    public const string Replace_01 = "        internal static partial Regex BaseHref();\n    }\n}";
}
