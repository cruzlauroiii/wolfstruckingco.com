return 0;

namespace Scripts
{
    internal static class PatchSourceWebsearchFixConfig
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\web-search.cs";
        public const string Find_01 = "        var End = Href.IndexOf('&', Q);";
        public const string Replace_01 = "#pragma warning disable MA0074\n        var End = Href.IndexOf('&', Q);\n#pragma warning restore MA0074";
    }
}
