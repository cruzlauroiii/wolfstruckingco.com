return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV34
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    var Html = await RenderRouteHtml(PageType);\r\n    var Slug = Route.Trim('/');\r\n    var OutDir = string.IsNullOrEmpty(Slug) ? Path.Combine(Repo, \"docs\") : Path.Combine(Repo, \"docs\", Slug.Replace('/', Path.DirectorySeparatorChar));\r\n    Directory.CreateDirectory(OutDir);\r\n    File.WriteAllText(Path.Combine(OutDir, \"index.html\"), Wrap(Slug, Html));\r\n\r\n    var Url = $\"{Base}{Route}?cb={Pad}\";";
        public const string Replace_01 = "    var Html = await RenderRouteHtml(PageType);\r\n    var ScenePathSlug = Route == \"/Chat/\" && SceneChat.ContainsKey(N) ? \"Chat/\" + Pad : Route.Trim('/');\r\n    var ScenePathRoute = Route == \"/Chat/\" && SceneChat.ContainsKey(N) ? \"/Chat/\" + Pad + \"/\" : Route;\r\n    var OutDir = string.IsNullOrEmpty(ScenePathSlug) ? Path.Combine(Repo, \"docs\") : Path.Combine(Repo, \"docs\", ScenePathSlug.Replace('/', Path.DirectorySeparatorChar));\r\n    Directory.CreateDirectory(OutDir);\r\n    File.WriteAllText(Path.Combine(OutDir, \"index.html\"), Wrap(ScenePathSlug, Html));\r\n\r\n    var Url = $\"{Base}{ScenePathRoute}?cb={Pad}\";";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
