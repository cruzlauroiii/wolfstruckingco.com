return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV300
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\scripts\generic\generate-statics.cs";
        public const string Find_01 = "        $\"<body data-prerender-route=\\\"{Slug}\\\">\",\n        \"<script>\",\n        SsoSnippet,\n        \"</script>\",\n        Body,\n        \"<script>\",";
        public const string Replace_01 = "        $\"<body data-prerender-route=\\\"{Slug}\\\">\",\n        \"<script>\",\n        SsoSnippet,\n        \"</script>\",\n        \"<div id=\\\"app\\\">\",\n        Body,\n        \"</div>\",\n        \"<script>\",";
    }
}
