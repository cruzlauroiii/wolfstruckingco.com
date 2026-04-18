return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV33
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\docs\videos\run-crud-pipeline.cs";
        public const string Find_01 = "    var Html = await RenderRouteHtml(PageType);\n    var Slug = Route.Trim('/');\n    var OutDir = string.IsNullOrEmpty(Slug) ? Path.Combine(Repo, \"docs\") : Path.Combine(Repo, \"docs\", Slug.Replace('/', Path.DirectorySeparatorChar));\n    Directory.CreateDirectory(OutDir);\n    File.WriteAllText(Path.Combine(OutDir, \"index.html\"), Wrap(Slug, Html));\n\n    var Url = $\"{Base}{Route}?cb={Pad}\";";
        public const string Replace_01 = "    var Html = await RenderRouteHtml(PageType);\n    var ScenePathSlug = Route == \"/Chat/\" && SceneChat.ContainsKey(N) ? \"Chat/\" + Pad : Route.Trim('/');\n    var ScenePathRoute = Route == \"/Chat/\" && SceneChat.ContainsKey(N) ? \"/Chat/\" + Pad + \"/\" : Route;\n    var OutDir = string.IsNullOrEmpty(ScenePathSlug) ? Path.Combine(Repo, \"docs\") : Path.Combine(Repo, \"docs\", ScenePathSlug.Replace('/', Path.DirectorySeparatorChar));\n    Directory.CreateDirectory(OutDir);\n    File.WriteAllText(Path.Combine(OutDir, \"index.html\"), Wrap(ScenePathSlug, Html));\n\n    var Url = $\"{Base}{ScenePathRoute}?cb={Pad}\";";
        public const string Find_02 = "___UNUSED___";
        public const string Replace_02 = "";
    }
}
